namespace Scaidome.OpcUa.Client;

public record ReadResult(string NodeId, object? Value, uint StatusCode, DateTime SourceTimestamp, DateTime ServerTimestamp, string? Error = null);
