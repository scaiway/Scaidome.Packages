# Scaidome.Abstractions

Shared types for exchanging values between Scaidome components. The project has no package references, so consumers of the OPC UA client never need a reference to the OPC Foundation SDK.

All types live in the `Scaidome` namespace.

## Types

| Type | Description |
|---|---|
| `DataValue` | A single value update: the address it came from, the value, its status code and timestamps |
| `DataValueChangedMessage` | A batch of `DataValue`s delivered together, e.g. through a `Channel<DataValueChangedMessage>` |
| `StatusCodes` | OPC UA status code constants plus helpers to check and name them |

### DataValue

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

### StatusCodes

Status codes are plain `uint` constants (`StatusCodes.Good`, `StatusCodes.BadNodeIdUnknown`, ...) that match the OPC UA specification.

```csharp
if (!StatusCodes.IsGood(value.StatusCode))
    Console.WriteLine($"Bad value: {StatusCodes.GetName(value.StatusCode)}");
```

- `IsGood(code)` is true for every Good code, including sub-codes such as `GoodClamped`. Uncertain and Bad codes return false.
- `GetName(code)` returns the symbolic name, e.g. `"BadNodeIdUnknown"`. The low 16 info bits are ignored when looking up the name, and an unknown code is returned as hex, e.g. `"0x80FF0000"`.
