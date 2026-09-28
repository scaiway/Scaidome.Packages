namespace Scaidome.OpcUa.Client;

public record WriteValue(string NodeId, object Value, string? TargetTypeNodeId = null);
