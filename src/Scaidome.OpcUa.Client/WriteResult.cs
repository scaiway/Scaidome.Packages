namespace Scaidome.OpcUa.Client;

public record WriteResult(string NodeId, uint StatusCode, string? Error = null);
