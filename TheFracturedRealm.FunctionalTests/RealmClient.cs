using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace TheFracturedRealm.FunctionalTests;

public sealed class RealmClient : IAsyncDisposable
{
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
                    line = await _reader.ReadLineAsync(_cts.Token);
                    lock (_recentLock)
                    {
                        ArgumentNullException.ThrowIfNull(line);
                        _recent.Enqueue(line);
                        while (_recent.Count > _logCapacity)
                        {
                            _recent.Dequeue();
                        }
                    }
                }
                catch (OperationCanceledException) when (_cts.IsCancellationRequested)
                {
                    break;
                }
                if (line is null)
                {
                    break;
                }
                try
                {
                    await _lines.Writer.WriteAsync(line, _cts.Token);
                }
                catch (OperationCanceledException) when (_cts.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        finally
        {
            _lines.Writer.TryComplete();
        }
    }
    public Task SendLineAsync(string line, CancellationToken ct = default)
        => _writer.WriteLineAsync(line.AsMemory(), ct);
    public async Task<string> WaitForLineAsync(Func<string, bool> predicate, TimeSpan timeout)
    {
        using var tcs = new CancellationTokenSource(timeout);
        while (await _lines.Reader.WaitToReadAsync(tcs.Token))
        {
            while (_lines.Reader.TryRead(out var line))
            {
                if (predicate(line))
                {
                    return line;
                }
            }
        }
        string recent;
        lock (_recentLock)
        {
            recent = string.Join('\n', _recent);
        }
        throw new TimeoutException($"Timed out after {timeout} waiting for a matching line. Recent log:\n{recent}");
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
        await _cts.CancelAsync();
        _client.Close();
        await _pump;
        _cts.Dispose();
        _reader.Dispose();
        await _writer.DisposeAsync();
        await _stream.DisposeAsync();
        _client.Dispose();
    }
    public async Task<IReadOnlyList<string>> WaitForLinesAsync(IReadOnlyList<Func<string, bool>> predicates, TimeSpan timeout, bool allowInterleaving = true)
    {
        using var tcs = new CancellationTokenSource(timeout);
        var matched = new List<string>(capacity: predicates.Count);
        var index = 0;
        while (index < predicates.Count && await _lines.Reader.WaitToReadAsync(tcs.Token))
        {
            while (index < predicates.Count && _lines.Reader.TryRead(out var line))
            {
                if (predicates[index](line))
                {
                    matched.Add(line);
                    index++;
                    continue;
                }
                if (!allowInterleaving)
                {
                    throw new InvalidOperationException($"Expected line {index + 1} of {predicates.Count}, but got: {line}");
                }
            }
        }
        if (index != predicates.Count)
        {
            string recent;
            lock (_recentLock)
            {
                recent = string.Join('\n', _recent);
            }
            throw new TimeoutException($"Timed out after {timeout} waiting for {predicates.Count} matching lines in order. Matched {index} lines. Recent log:\n{recent}");
        }
        return matched;
    }
    public Task<IReadOnlyList<string>> ExpectAsync(TimeSpan timeout, params string[] containsInOrder)
    {
        static string Plain(string s) => Sanitizer.StripAnsi(s);
        var predicates = containsInOrder.Select(expected => (Func<string, bool>)(line => Plain(line).Contains(expected, StringComparison.OrdinalIgnoreCase))).ToArray();
        return WaitForLinesAsync(predicates, timeout, allowInterleaving: true);
    }
}
