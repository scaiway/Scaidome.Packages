# Scaidome.Abstractions

The value types and rules that every Scaidome component shares: values and their conversions, declared types, status
codes, value updates, name rules and a small metrics abstraction. The project has no package references, so consumers
of the OPC UA client never need a reference to the OPC Foundation SDK.

## Namespaces

The project uses two namespaces on purpose:

- `Scaidome`: values, their conversions, declared types, status codes and value updates. These are used almost
  everywhere, so they sit in the root namespace.
- `Scaidome.Abstractions`: the name rules and the metrics abstraction.

## Values

A `Variant` is one piece of data together with its `VariantKind`. The kinds are no value, whole number, number, boolean,
epoch, text, JSON, colour, matrix and date-time.

- **Storage.** Each kind is held in one of three ways (`VariantStorage`): as a number (whole number, number, boolean,
  epoch), as text (text, JSON, colour, matrix) or as a date-time. Accessors check the storage, not the kind: `AsDouble`
  works on a boolean and `AsText` works on a colour. An accessor for the wrong storage throws.
- **A variant never holds what its kind cannot mean.** The `From…` factories throw on a non-finite number or on text
  that is not well-formed JSON. Input you don't trust goes through `VariantConversion` or `VariantTextForm`, which
  report failure instead of throwing.
- **Times are UTC.** A local time is converted. A time with no kind is taken to be UTC already, because the stores hold
  UTC and nothing says which zone it was written in.
- **Equality compares kind and content.** Whole number `1` is not equal to number `1`. Comparing with a plain CLR value
  (`variant.Equals(1)`) compares by storage instead, so a number-stored value equals the same numeric value.
- **Text needs a kind.** Text could mean any of four kinds, so a plain string never becomes a variant on its own. Name
  the kind with `FromText(kind, text)`.
- **`FromTrustedJson`** skips the JSON check. Use it only for text that a parser or writer produced within the same
  operation.
- `ZeroOf(kind)` gives the value that stands in when a value of a kind is needed and none is configured.

### Conversions

| Type | Purpose |
|---|---|
| `VariantConversion` | Converts a variant or a plain CLR value to a declared kind, or reports failure (`TryConvert`/`Convert`). A value already of the kind is unchanged. A number never converts to a date-time, because the epoch's unit belongs to the interface. |
| `VariantTextForm` | Writes a value as its text and its kind's name, and reads it back, so the kind survives being stored as text. Reading reports failure, so a record that can't be read can be skipped. Only an exact kind name is accepted, never a number or a list of names. |
| `VariantJsonConverter` | The JSON form, attached to `Variant`. It drops the kind: read back, a number that fits 32 bits becomes a whole number, other numbers become numbers, strings become text, and objects and arrays become JSON. A refused value is reported as malformed JSON. |

## Declared types

`DeclaredTypes` holds the nine type words a definition uses to declare a value's kind: `int`, `number`, `boolean`,
`string`, `json`, `datetime`, `epoch`, `color` and `matrix`. It maps each word to a `VariantKind` and back. Words match
without regard to case and are kept in lower case. A stored word that is not a type reads as text. Checking a given
value against its declared type is not done here; that belongs to the settings API.

## Status codes

`StatusCodes` holds the OPC UA quality codes as `uint` constants (`StatusCodes.Good`, `StatusCodes.BadNodeIdUnknown`,
...), each with OPC UA's own description, together with checks on the top two bits: `IsGood` (00), `IsUncertain` (01),
`IsBad` (10) and `IsReserved` (11). Only the top bits decide a code's class; the lower bits carry detail, so a Good
sub-code such as `GoodClamped` is still good.

```csharp
if (!StatusCodes.IsGood(value.StatusCode))
    Console.WriteLine($"Bad value: {StatusCodes.GetName(value.StatusCode)}");
```

`GetName(code)` returns the symbolic name, e.g. `"BadNodeIdUnknown"`. The low 16 info bits are ignored when looking up
the name, and an unknown code is returned as hex, e.g. `"0x80FF0000"`.

## Value updates

| Type | Description |
|---|---|
| `DataValue` | A single value update: the address it came from, the value, its status code and timestamps |
| `DataValueChangedMessage` | A batch of `DataValue`s delivered together, e.g. through a `Channel<DataValueChangedMessage>` |

```csharp
public record DataValue(string Address, object? Value, uint StatusCode, DateTime SourceTimestamp, DateTime ServerTimestamp, object Context);
```

| Property | Description |
|---|---|
| `Address` | Where the value came from. For OPC UA this is the node id, e.g. `ns=2;s=Temperature` |
| `Value` | The value, as a .NET type (`int`, `double`, `string`, arrays, ...) or `null` |
| `StatusCode` | The OPC UA status code; see `StatusCodes` |
| `SourceTimestamp` | When the data source produced the value |
| `ServerTimestamp` | When the server received the value |
| `Context` | Producer-specific data. The OPC UA client puts the `MonitoredNode` here, and that object in turn carries the context you gave when you started monitoring. Use it to route updates without a lookup |

## Name rules

`NameRules` is the one implementation of the rules every model applies to names. A check returns `null` when the name is
valid and the refusal message otherwise. The messages are public constants, so tests and callers can match them.

- **Canonical form.** An ideographic space becomes a plain space, then the text is Unicode-composed (NFC), then trimmed,
  in that order. `CanonicalizeAndCheck` does both steps and hands back the form to store.
- **Character forms** (`NameForm`):
  - `Loose` (the default) allows letters, digits, underscores, hyphens and spaces. It applies to areas, topology
    records, user-defined types and evaluator templates.
  - `TagName` allows letters, digits and underscores.
  - `DottedIdentifier` is tag-name segments joined by dots, with no empty segment. It applies to property names.
- **Invisible and formatting characters** are refused, so two names that look identical can't both exist.
- **Writing systems.** Letters and digits must come from Latin, Greek, Cyrillic, Hebrew, Arabic, Devanagari, Thai,
  Chinese, Japanese or Korean. A name may not mix writing systems, except in the combinations CJK text really uses
  (each of which may also contain Latin).
- **Order of refusal.** Every problem is found in one scan, and the first reported is, in order: missing, invisible
  characters, bad characters, unsupported writing system, mixed writing systems, empty dotted segment.
- **The scan runs by code point, not by regular expression.** A regular expression sees a character outside the basic
  plane as two halves.
- **Uniqueness.** `ComparisonKey` is the key names are compared through. It composes and trims the name, then
  lower-cases and upper-cases it in the invariant culture. `NamesEqual` compares two names by their keys.
- **Prose.** `CanonicalizeProse` composes and trims descriptions, alarm labels and engineering units, with no character
  rule.

## Metrics

`IMeterFactory`, `IMeter` and `Measurement<T>` keep the platform's own metrics types out of the libraries' surface, so
the host decides how metrics are collected and exported. They cover observable gauges and counters only, and a callback
runs only when a collector asks.

`DiagnosticsMeterFactory` is the implementation over `System.Diagnostics.Metrics`. If you give it the platform's
`IMeterFactory`, it creates meters through that factory, so the host's collectors see them. Disposing the factory
disposes every meter it created.
