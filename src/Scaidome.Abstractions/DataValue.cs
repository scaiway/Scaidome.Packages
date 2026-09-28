
namespace Scaidome;

public record DataValue(string Address, object? Value, uint StatusCode, DateTime SourceTimestamp, DateTime ServerTimestamp, object Context);
