namespace Scaidome.OpcUa.Client;

public record NodeAttributes(
    string NodeId,
    string DisplayName,
    object? Value,
    string DataType,
    byte AccessLevel,
    byte UserAccessLevel,
    bool CanWrite,
    bool CanRead,
    int ValueRank,
    string? Description,
    uint StatusCode = 0,
    string? Error = null
);
