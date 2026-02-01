# The Fractured Realm

A tiny, hackable, telnet-style multiplayer text realm written in C#.

This repo hosts a TCP server and a simple command loop where connected sessions can pick a handle, speak, and see who else is online. Output uses ANSI escape codes for color/formatting, and inbound text is sanitized to prevent ANSI/control-sequence injection.

## What it does

- Listens for TCP connections on **port 4000**
- Creates a `Session` per client (with an outbound message channel)
- Routes inbound lines through a single logical game loop (`Channel<InboundMessage>`)
- Dispatches commands via a lightweight command system (`ICommand` + `CommandDispatcher`)
- Broadcasts chat/notifications to all connected sessions (`World.Broadcast`)
- Provides a few starter commands:
    - `name <yourname>` — set your displayed handle (unique, max 20 chars)
    - `look` (alias: `l`) — look around and see who else is present
    - `who` — list connected players
    - `help [command]` — show command list or detailed usage
    - `say <message>` (alias: `s`) — speak to everyone
        - Shorthand: a line starting with `'` is treated as `say` (e.g. `'hello`)

If you type something that doesn’t match a command, it falls back to `say`.

## Requirements

- ncat (choco install nmap)
- .NET SDK capable of building **`net10.0`** (see `TheFracturedRealm.csproj`)

## Run locally

From the repo root:

```bash
dotnet run --project TheFracturedRealm
```

You should see a log line indicating the server is listening on port 4000.

## Connect to the server

From another terminal:

```bash
ncat localhost 4000
```

You’ll get a welcome message and be prompted to set a handle:

```text
name Alice
```

Then try:

```text
look
who
'hello everyone
help
help say
```

## Project structure (high level)

- `Program.cs`
  Configures a generic host, DI, and two hosted services:
    - `TcpServerService` (networking)
    - `GameLoopService` (command processing)

- `TcpServerService`
  Accepts TCP clients, spawns a per-client writer loop, reads lines from the socket, and pushes them into the inbound channel.

- `GameLoopService`
  Single reader loop over the inbound channel. Dispatches commands and handles errors.

- `World`
  Tracks active sessions and broadcasts messages.

- `CommandDispatcher`
  Maintains a list of commands, selects the first one that matches a parsed input message to execute it asynchronously, and if none match, falls back to a default “say” command when available.

- `Session`
  Wraps the `TcpClient` and owns an outbound `Channel<OutboundMessage>`.

- `Features/*Command.cs`
  Built-in commands.

- `Abstractions/ICommand.cs`
  Minimal command contract:
    - `Name`, optional `Aliases`, `Usage`, `Summary`
    - `Matches(CommandInput)` helper (verb-based)
    - `ExecuteAsync(CommandContext, CommandInput, CancellationToken)`

- `Ansi` + `Sanitizer`
    - `Ansi` defines escape sequences for styling output.
    - `Sanitizer` strips ANSI/control sequences from user input and forces it to one line.

## Architecture

This project implements a concurrent, thread-safe multiplayer game server using a channel-based architecture that separates networking concerns from game logic. The design focuses on deterministic command processing while safely handling multiple concurrent client connections.

### Single Logical Game Loop

The core game logic runs in a **single logical execution context** via `GameLoopService`, which continuously reads from a shared `Channel<InboundMessage>`. This design choice provides several critical benefits:

- **Deterministic processing:** All commands execute in the order they're received, eliminating race conditions when modifying shared game state (player names, session lists, etc.)
- **Simplified state management:** No locks or concurrent collections needed for game state since only one thread mutates it
- **Predictable behavior:** Message ordering is guaranteed, so commands like "name Alice" followed by "say hello" always execute in that sequence

The game loop implementation in `GameLoopService.ExecuteAsync` is straightforward:

```csharp
while (await reader.WaitToReadAsync(stoppingToken))
{
    while (reader.TryRead(out var inbound))
    {
        await HandleInbound(inbound, stoppingToken);
    }
}
```

Each inbound message is processed synchronously and completely before moving to the next. Commands can be async (for I/O operations), but only one command executes at a time per the game loop's single-reader pattern.

### Channel-Based Message Flow

The system uses .NET `System.Threading.Channels` for lock-free, high-performance message passing between components:

#### Inbound Flow (Client → Game Loop)

1. `TcpServerService` accepts TCP connections and spawns a reader task per client
2. Each client's reader task parses incoming lines from the network stream
3. Messages are written to a **shared unbounded channel** `Channel<InboundMessage>`
4. The single logical game loop reads from this channel and dispatches commands

This decouples network I/O (potentially many concurrent clients) from command processing (single-threaded, ordered).

```
[Client 1] ──┐
[Client 2] ──┤ TcpServerService reader tasks
[Client 3] ──┤    │
    ...      ──┘    ├──> Channel<InboundMessage> ──> GameLoopService (single thread)
```

#### Outbound Flow (Game Loop → Clients)

Each `Session` owns a **private unbounded channel** `Channel<OutboundMessage>` with:

- **Single reader:** the session's dedicated writer loop (in `WriterLoopAsync`)
- **Multiple writers:** the game loop, broadcast operations, or any command can write to it

When the game loop or a command wants to send output to a client:

1. It calls `session.OutboundWriter.TryWrite(new OutboundMessage(text))`
2. The session's writer loop (running on a separate task) reads from the channel
3. Messages are serialized to the TCP stream in order

```
GameLoopService ──┬──> Session1.OutboundChannel ──> WriterLoopAsync ──> [Client 1]
                  ├──> Session2.OutboundChannel ──> WriterLoopAsync ──> [Client 2]
                  └──> Session3.OutboundChannel ──> WriterLoopAsync ──> [Client 3]
```

Broadcasts iterate over all sessions and write to each session's outbound channel, allowing the game loop to dispatch messages without blocking on network I/O.

### Thread Safety Approach

The architecture achieves thread safety through **isolation and message passing** rather than locks:

#### 1. Session State Isolation

- Each `Session` object is only mutated by the game loop thread
- Session properties like `Name` are set during command execution (name command)
- Network I/O tasks only _read_ session data or write to channels (lock-free operations)

#### 2. Channel Synchronization

- Channels provide thread-safe enqueue/dequeue operations internally
- The `Channel<InboundMessage>` is unbounded, so `TryWrite` never blocks or fails due to capacity
- Outbound channels are also unbounded with `SingleReader = true, SingleWriter = false`

#### 3. World State Management

- `World` uses a `ConcurrentDictionary<Guid, Session>` for the session registry
- Add/Remove operations are thread-safe (called from `TcpServerService`)
- `SnapshotSessions()` creates a point-in-time array copy for safe iteration
- The game loop only _reads_ sessions from `World`; mutations happen via channels

#### 4. Safe Broadcasts

When broadcasting to all clients:

```csharp
foreach (var session in _sessions.Values)
{
    session.OutboundWriter.TryWrite(new OutboundMessage(line));
}
```

This is safe because:

- The `ConcurrentDictionary` allows safe concurrent reads
- `TryWrite` is thread-safe and non-blocking
- Even if a session disconnects mid-broadcast, the completed channel simply drops writes

#### 5. Input Sanitization

- All user input passes through `Sanitizer.SafeText` to strip ANSI escape codes and control characters
- This prevents injection attacks and ensures terminals display output safely
- Sanitization happens in the game loop before any processing

### Summary

This architecture demonstrates modern .NET concurrency best practices:

- Use channels for inter-thread communication instead of shared mutable state
- Confine state mutations to a single thread where possible
- Reserve concurrent collections (`ConcurrentDictionary`) only for registry/lookup scenarios
- Keep I/O tasks (network reads/writes) separate from business logic (command processing)

The result is a system that scales to many concurrent connections while maintaining simple, predictable game state behavior.

## Extending the realm

Add a new command:

1. Create a class that implements `ICommand` (see `Features/LookCommand.cs` for a simple example).
2. Register it in `CommandDispatcher`:

```csharp
public void RegisterExistingCommands()
{
    Register(new NameCommand());
    Register(new LookCommand());
    Register(new WhoCommand());
    Register(new SayCommand());
    Register(new HelpCommand(() => Commands));
    Register(new MyCommand());
}
```

Tips:

- Use `ctx.Reply(...)` for responses to the current session.
- Use `ctx.Broadcast(...)` to message everyone (optionally excluding the sender).
- Sanitize any user-provided text with `Sanitizer.SafeText(...)`.

## Troubleshooting

- **No colors / weird characters:** your client may not support ANSI escape codes.
- **Can’t connect:** ensure port **4000** is open and not in use.
- **Nothing happens when typing:** make sure your client sends newline-terminated lines (telnet/nc do).
