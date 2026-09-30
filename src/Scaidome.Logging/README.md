# Scaidome.Logging

A logging provider for `Microsoft.Extensions.Logging` that writes to a rolling file. Depends on
`Microsoft.Extensions.Logging.Abstractions`. Namespace: `Scaidome.Logging.File`.

## Usage

The host binds the options from configuration and constructs the provider itself:

```csharp
var fileLogging = new FileLoggerProvider(options);
builder.Logging.AddProvider(fileLogging);

// The file keeps its own levels, so the framework passes everything through and the file's loggers decide.
builder.Logging.AddFilter<FileLoggerProvider>(null, LogLevel.Trace);

// ... run the host ...

fileLogging.Dispose(); // last, after the host has stopped
```

Construct the provider before the host is built and dispose it after the host has stopped. The last lines before a
shutdown often explain it, and a provider disposed by the container could be gone before those lines are written.

## Options

`FileLoggerOptions` is usually bound from a `File` section under `Logging`. The provider's alias is `File`.

| Setting | Default | Meaning |
|---|---|---|
| `Path` | `/logs/app.log` | The live file. A host resolves a relative path itself, before it hands the options over. |
| `FileSizeLimitBytes` | 10 MB | The size at which the live file rolls |
| `MaxRollingFiles` | 3 | The number of files kept, counting the live file |
| `LogLevel` | `Default: Information` | Levels by category, compared without regard to case |

A logger's level is decided once, when the logger is created. The longest configured category that the logger's
category starts with decides the level; otherwise `Default` does, and it is Information if not given.

## Behaviour

- **Callers never wait on the file.** Every logger queues its entries, and one background thread writes them to the
  file in order.
- **Nothing holds the file between entries.** It is opened for each entry and closed again, and it is shared for
  reading and deleting.
- **Rolling happens before a write.** When the live file has reached the size limit, the oldest numbered file is
  deleted, the other numbered files move up one (`app.log.1` becomes `app.log.2`), and the live file becomes
  `app.log.1`. With `MaxRollingFiles` at 1, the live file is simply deleted.
- **Logging never fails its caller.** A failed write is dropped, because there is nowhere else to report it. An entry
  logged after disposal is dropped too.
- **Disposal waits, but not forever.** It waits up to two seconds for queued entries to be written, then cancels. Any
  entries not written by then are dropped.
- **Entry format.** Local time with its offset, the level in brackets, the category (with a non-zero event id in
  brackets), then a colon and the message. The exception's full text follows on the next lines.

  ```
  2026-09-26 11:08:36.123+02:00 [Warning] Scai.Core.Auth[12]: Message text
  ```

## Known defect

Scopes aren't supported: `BeginScope` returns `null`, so a caller that disposes the result must allow for null. The fix
is to return a scope object that does nothing. A test pins the current behaviour.
