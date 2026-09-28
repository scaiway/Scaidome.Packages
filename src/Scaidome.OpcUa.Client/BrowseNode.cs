using System.Text.Json.Serialization;

namespace Scaidome.OpcUa.Client;

public record BrowseNode
{
    public BrowseNode(string displayName, string nodeId, bool isFolder, string nodeClass, string browseName, Type? dataType)
    {
        DisplayName = displayName;
        NodeId = nodeId;
        IsFolder = isFolder;
        NodeClass = nodeClass;
        BrowseName = browseName;
        DataType = dataType;
    }
    public string DisplayName { get; init; }
    public string NodeId { get; init; }
    public bool IsFolder { get; init; }
    public string NodeClass { get; init; }
    public string BrowseName { get; init; }

    [JsonIgnore]
    public Type? DataType { get; init; }

    [JsonPropertyName("dataType")]
    public string? DataTypeName => DataType?.Name;
}
