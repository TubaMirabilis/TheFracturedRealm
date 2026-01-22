# The Fractured Realm

A tiny, hackable, telnet-style multiplayer text realm written in C#.

This repo hosts a TCP server and a simple command loop where connected sessions can pick a handle, speak, and see who else is online. Output uses ANSI escape codes for color/formatting, and inbound text is sanitized to prevent ANSI/control-sequence injection.

## What it does

- Listens for TCP connections on **port 4000**
- Creates a `Session` per client (with an outbound message channel)
- Routes inbound lines through a single-threaded game loop (`Channel<InboundMessage>`)
- Dispatches commands via a lightweight command system (`ICommand` + `CommandDispatcher`)
- Broadcasts chat/notifications to all connected sessions (`World.Broadcast`)
- Provides a few starter commands:
    - `name <yourname>` — set your displayed handle (unique, max 20 chars)
    - `look` (alias: `l`) — look around and see who else is present
    - `who` — list connected players
    - `help [command]` — show command list or detailed usage
    - `say <message>` (aliases: `s`, `'`) — speak to everyone
        - If a line starts with `'` it’s treated as `say` (e.g. `'hello`)

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

- `Session`
  Wraps the `TcpClient` and owns an outbound `Channel<OutboundMessage>`.

- `Features/*Command.cs`
  Built-in commands.

- `Abstractions/ICommand.cs`
  Minimal command contract:
    - `Name`, optional `Aliases`, `Usage`, `Summary`
    - `Matches(line)` helper (verb-based, can be overridden)
    - `ExecuteAsync(...)`

- `Ansi` + `Sanitizer`
    - `Ansi` defines escape sequences for styling output.
    - `Sanitizer` strips ANSI/control sequences from user input and forces it to one line.

## Design notes

- **Single-threaded “game loop”:** inbound messages are processed by one reader for deterministic command handling.
- **Per-session outbound channels:** each session has a writer loop that serializes messages to the socket.
- **Safety:** user input is sanitized (`Sanitizer.SafeText`) to avoid terminal escape/OSC injection and control characters.
- **Command ergonomics:** verbs are extracted by splitting on the first space; `say` supports shorthand `'message`.

## Extending the realm

Add a new command:

1. Create a class that implements `ICommand` (see `Features/LookCommand.cs` for a simple example).
2. Register it in `GameLoopService`’s constructor:

```csharp
_dispatcher.Register(new MyCommand());
```

Tips:

- Use `ctx.Reply(...)` for responses to the current session.
- Use `ctx.Broadcast(...)` to message everyone (optionally excluding the sender).
- Sanitize any user-provided text with `Sanitizer.SafeText(...)`.

## Troubleshooting

- **No colors / weird characters:** your client may not support ANSI escape codes.
- **Can’t connect:** ensure port **4000** is open and not in use.
- **Nothing happens when typing:** make sure your client sends newline-terminated lines (telnet/nc do).
