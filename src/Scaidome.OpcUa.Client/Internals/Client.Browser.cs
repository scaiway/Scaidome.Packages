using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Opc.Ua;

namespace Scaidome.OpcUa.Client;

internal partial class Client
{
    public async Task<BrowseNode[]> BrowseAsync(string nodeId, bool includeDataType = false, CancellationToken ct = default)
    {
        if (_session == null || _session.Connected == false)
            throw new InvalidOperationException("Session is not connected");
        if (string.IsNullOrWhiteSpace(nodeId))
            throw new ArgumentNullException(nameof(nodeId));

        using Activity? activity = _telemetry.StartActivity();
        try
        {
            var nodeList = new List<BrowseNode>();
            var visited = new HashSet<string>();
            var variableNodes = new List<(int index, NodeId nodeId)>();

            var browseDescription = new BrowseDescription
            {
                NodeId = ExpandedNodeId.ToNodeId(nodeId, _session.NamespaceUris),
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                IncludeSubtypes = true,
                NodeClassMask = 0,
                ResultMask = (uint)BrowseResultMask.All
            };
            var browseDescriptions = new BrowseDescriptionCollection { browseDescription };

            var browseResponse = await _session.BrowseAsync(null, null, 0, browseDescriptions, ct).ConfigureAwait(false);
            var allReferences = browseResponse.Results[0].References;
            var continuationPoint = browseResponse.Results[0].ContinuationPoint;
            while (continuationPoint != null)
            {
                var continuationPoints = new ByteStringCollection { continuationPoint };
                var browseNextResponse = await _session.BrowseNextAsync(null, false, continuationPoints, ct).ConfigureAwait(false);
                allReferences.AddRange(browseNextResponse.Results[0].References);
                continuationPoint = browseNextResponse.Results[0].ContinuationPoint;
            }

            // First pass: create nodes with basic info
            foreach (var reference in allReferences)
            {
                var node = new BrowseNode(
                    displayName: reference.DisplayName.Text,
                    nodeId: reference.NodeId.ToString(),
                    isFolder: reference.NodeClass == NodeClass.Object,
                    nodeClass: reference.NodeClass.ToString(),
                    browseName: reference.BrowseName.ToString(),
                    dataType: null
                );

                // avoid duplicates
                if (!visited.Contains(node.NodeId))
                {
                    visited.Add(node.NodeId);
                    nodeList.Add(node);

                    // Track Variable nodes for DataType retrieval (only if includeDataType == true)
                    if (includeDataType && reference.NodeClass == NodeClass.Variable)
                    {
                        variableNodes.Add((nodeList.Count - 1, ExpandedNodeId.ToNodeId(reference.NodeId, _session.NamespaceUris)));
                    }
                }
            }

            // Second pass: batch read DataType for Variable nodes (only if includeDataType == true)
            if (includeDataType && variableNodes.Any())
            {
                var nodesToRead = new ReadValueIdCollection(
                    variableNodes.Select(v => new ReadValueId
                    {
                        NodeId = v.nodeId,
                        AttributeId = Attributes.DataType
                    })
                );

                var readResponse = await _session.ReadAsync(null, 0, TimestampsToReturn.Neither, nodesToRead, ct).ConfigureAwait(false);

                for (int i = 0; i < variableNodes.Count; i++)
                {
                    if (StatusCode.IsGood(readResponse.Results[i].StatusCode) && readResponse.Results[i].Value != null)
                    {
                        var (index, _) = variableNodes[i];
                        var dataTypeNodeId = (NodeId)readResponse.Results[i].Value;

                        // Convert OPC UA DataType NodeId to CLR System.Type
                        var clrType = Opc.Ua.TypeInfo.GetSystemType(dataTypeNodeId, _session.Factory);

                        // Handle Variant type
                        if (clrType == typeof(Variant))
                            clrType = typeof(object);

                        var oldNode = nodeList[index];
                        nodeList[index] = oldNode with { DataType = clrType };
                    }
                }
            }

            // Sort by: folders first, then by display name
            nodeList.Sort((a, b) =>
            {
                if (a.IsFolder != b.IsFolder)
                    return b.IsFolder.CompareTo(a.IsFolder); // true (folders) before false (variables)
                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
            });

            return nodeList.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BrowseAsync failed for node {NodeId}", nodeId);
            throw;
        }
    }
}
