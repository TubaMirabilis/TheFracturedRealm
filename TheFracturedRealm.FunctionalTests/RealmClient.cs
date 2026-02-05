using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace TheFracturedRealm.FunctionalTests;

public sealed class RealmClient : IAsyncDisposable
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
    public static readonly StringComparison DefaultComparison = StringComparison.OrdinalIgnoreCase;
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly Channel<string> _lines = Channel.CreateUnbounded<string>();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _pump;
    private readonly int _logCapacity = 200;
    private readonly Queue<string> _recent = new();
    private readonly Lock _recentLock = new();
    public RealmClient(string host = "127.0.0.1", int port = 4000)
    {
        _client = new TcpClient
        {
            NoDelay = true
        };
        _client.Connect(host, port);
        _stream = _client.GetStream();
        _reader = new StreamReader(_stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        _writer = new StreamWriter(_stream, new UTF8Encoding(false), bufferSize: 4096, leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\r\n"
        };
        _pump = Task.Run(PumpAsync);
    }
    private async Task PumpAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                string? line;
                try
                {
                    line = await _reader.ReadLineAsync(_cts.Token).ConfigureAwait(false);
                    if (line is null)
                    {
                        break;
                    }

                    lock (_recentLock)
                    {
                        _recent.Enqueue(line);
                        while (_recent.Count > _logCapacity)
                        {
                            _recent.Dequeue();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // treat any OCE as shutdown for the pump
                    break;
                }
                catch (ObjectDisposedException) { break; }
                catch (IOException) { break; }

                try
                {
                    await _lines.Writer.WriteAsync(line, _cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch
        {
            // Swallow anything unexpected so DisposeAsync doesn't randomly blow up the test.
            // If you want, store it somewhere for debugging.
        }
        finally
        {
            _lines.Writer.TryComplete();
        }
    }
    public Task SendLineAsync(string line, CancellationToken ct = default)
        => _writer.WriteLineAsync(line.AsMemory(), ct);
    public async Task<string> WaitForLineAsync(
    Func<string, bool> predicate,
    TimeSpan timeout,
    CancellationToken ct = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            _cts.Token, ct, timeoutCts.Token);

        try
        {
            while (true)
            {
                // Fast path: consume anything already queued
                while (_lines.Reader.TryRead(out var line))
                {
                    if (predicate(line))
                    {
                        return line;
                    }
                }

                // Slow path: wait for exactly one next line
                var next = await _lines.Reader.ReadAsync(linked.Token).ConfigureAwait(false);
                if (predicate(next))
                {
                    return next;
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !_cts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw BuildTimeout(timeout, "a matching line");
        }
        catch (ChannelClosedException)
        {
            // Producer is done; treat as timeout-ish but include log
            throw BuildTimeout(timeout, "a matching line (channel closed)");
        }
    }

    private TimeoutException BuildTimeout(TimeSpan timeout, string waitingFor)
    {
        string recent;
        lock (_recentLock)
        {
            recent = string.Join('\n', _recent);
        }

        return new TimeoutException(
            $"Timed out after {timeout} waiting for {waitingFor}. Recent log:\n{recent}");
    }
    public async Task<IReadOnlyList<string>> DrainAsync(TimeSpan duration)
    {
        var results = new List<string>();
        using var tcs = new CancellationTokenSource(duration);
        try
        {
            while (await _lines.Reader.WaitToReadAsync(tcs.Token))
            {
                while (_lines.Reader.TryRead(out var line))
                {
                    results.Add(line);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        return results;
    }
    public async ValueTask DisposeAsync()
    {
        try { await _cts.CancelAsync().ConfigureAwait(false); } catch { /* ignore */ }
        try { _client.Close(); } catch { /* ignore */ }
        try { await _pump.ConfigureAwait(false); } catch { /* ignore */ }
        _cts.Dispose();
        _reader.Dispose();
        try { await _writer.DisposeAsync().ConfigureAwait(false); } catch { /* ignore */ }
        try { await _stream.DisposeAsync().ConfigureAwait(false); } catch { /* ignore */ }
        _client.Dispose();
    }
    public async Task<IReadOnlyList<string>> WaitForLinesAsync(
    IReadOnlyList<Func<string, bool>> predicates,
    TimeSpan timeout,
    bool allowInterleaving = true,
    CancellationToken ct = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            _cts.Token, ct, timeoutCts.Token);

        var matched = new List<string>(predicates.Count);
        var index = 0;

        try
        {
            while (index < predicates.Count)
            {
                // drain buffered
                while (_lines.Reader.TryRead(out var line))
                {
                    if (predicates[index](line))
                    {
                        matched.Add(line);
                        index++;
                        if (index == predicates.Count)
                        {
                            return matched;
                        }

                        continue;
                    }

                    if (!allowInterleaving)
                    {
                        throw new InvalidOperationException(
                            $"Expected line {index + 1} of {predicates.Count}, but got: {line}");
                    }
                }

                // wait for one more line
                var next = await _lines.Reader.ReadAsync(linked.Token).ConfigureAwait(false);

                if (predicates[index](next))
                {
                    matched.Add(next);
                    index++;
                }
                else if (!allowInterleaving)
                {
                    throw new InvalidOperationException(
                        $"Expected line {index + 1} of {predicates.Count}, but got: {next}");
                }
            }

            return matched;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !_cts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw BuildTimeout(timeout, $"{predicates.Count} matching lines in order. Matched {index}");
        }
        catch (ChannelClosedException)
        {
            throw BuildTimeout(timeout, $"{predicates.Count} matching lines (channel closed). Matched {index}");
        }
    }
    public Task<IReadOnlyList<string>> ExpectAsync(TimeSpan timeout, params string[] containsInOrder)
    {
        static string Plain(string s) => Sanitizer.StripAnsi(s);
        var predicates = containsInOrder.Select(expected => (Func<string, bool>)(line => Plain(line).Contains(expected, StringComparison.OrdinalIgnoreCase))).ToArray();
        return WaitForLinesAsync(predicates, timeout, allowInterleaving: true);
    }
    public static Func<string, bool> ContainsPredicate(string expected, StringComparison comparison = default)
    {
        comparison = comparison == default ? DefaultComparison : comparison;
        return line => Sanitizer.StripAnsi(line).Contains(expected, comparison);
    }
    public Task<string> WaitForContainsAsync(
    string expected,
    TimeSpan? timeout = null,
    StringComparison comparison = default,
    CancellationToken ct = default)
    => WaitForLineAsync(ContainsPredicate(expected, comparison), timeout ?? DefaultTimeout, ct);
    public async Task<string> SendAndWaitAsync(string command, string expectedResponse, CancellationToken ct, TimeSpan? timeout = null)
    {
        await SendLineAsync(command, ct);
        return await WaitForContainsAsync(expectedResponse, timeout);
    }
    public static async Task<RealmClient> ConnectAtPromptAsync(string host = "127.0.0.1", int port = 4000)
    {
        var client = new RealmClient(host, port);
        await client.WaitForContainsAsync("Welcome to The Fractured Realm!");
        await client.WaitForContainsAsync("Your handle? Type: name <yourname>");
        return client;
    }
    public static async Task<RealmClient> ConnectAndNameAsync(string name, string host = "127.0.0.1", int port = 4000, CancellationToken ct = default)
    {
        var client = await ConnectAtPromptAsync(host, port);
        await client.SendAndWaitAsync($"name {name}", $"Welcome, {name}!", ct);
        return client;
    }
}
