# Scaidome.Dapper

Dapper reading rules for the SQLite stores: how an identity and a time held as text are read back. Depends on Dapper.
Namespace: `Scaidome.Dapper`.

## Usage

Call this once, before the first SQLite connection is opened:

```csharp
SqliteValueTypes.Register();
```

Dapper keeps its type handlers per process, so the rules apply to every SQLite store in the process. Every part that
opens a store registers them before its first connection. This way the rules are in place whichever part opens a store
first. Registering again changes nothing, and registration clears Dapper's query cache: a reader compiled before the
handlers existed would otherwise keep reading without them.

## Identities

`IdentityTypeHandler` reads a `Guid` that SQLite holds as text, in any case. It also accepts a `Guid` or a 16-byte blob.
Text that is not an identity throws a `DataException` instead of yielding an empty identity, because a silently empty
identity would point a row at nothing.

## Times

`TimeTypeHandler` reads a `DateTime` held as text, always as UTC and whatever the machine's culture.

- It accepts the driver's own form: date, a space, then the time with up to seven fractional digits and no zone. It
  also accepts the ISO forms with `T`, with or without fractions or a trailing `Z`, which is what hand-written rows and
  other tools leave behind.
- Any other text that reads as a date and time is read as UTC too; text with an offset is converted using that offset.
- A value with no zone is taken to be UTC. A local value is converted.
- Text that is not a time throws a `DataException`.

`TimeTypeHandler.Format` writes a time in the rules' own form, `yyyy-MM-ddTHH:mm:ss.fffZ`, in the invariant culture.

## Writing parameters

Both handlers only read. When writing, the parameter keeps the `Guid` or `DateTime` itself, so the database driver binds
it in its own form, which is the form every store holds.
