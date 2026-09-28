using Microsoft.Extensions.Logging;
using Opc.Ua;
using System.Globalization;
using System.Text.Json;

namespace Scaidome.OpcUa.Client;

internal partial class Client
{
    public async Task<ReadResult> ReadValueAsync(string nodeId, CancellationToken ct = default)
    {
        try
        {
            if (_session == null || _session.Connected == false)
                return new ReadResult(nodeId, null, StatusCodes.BadNotConnected, DateTime.MinValue, DateTime.MinValue, "Session is not connected");

            var node = NodeId.Parse(nodeId);
            var nodesToRead = new ReadValueIdCollection
            {
                new ReadValueId { NodeId = node, AttributeId = Attributes.Value }
            };
            var response = await _session.ReadAsync(null, 0, TimestampsToReturn.Both, nodesToRead, ct).ConfigureAwait(false);
            ClientBase.ValidateResponse(response.Results, nodesToRead);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, nodesToRead);
            var data = response.Results[0];
            return new ReadResult(nodeId, data.Value, data.StatusCode.Code, data.SourceTimestamp, data.ServerTimestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReadValueAsync failed for node {NodeId}", nodeId);
            return new ReadResult(nodeId, null, StatusCodes.Bad, DateTime.MinValue, DateTime.MinValue, ex.Message);
        }
    }

    public async Task<NodeAttributes> ReadNodeAttributesAsync(string nodeId, CancellationToken ct = default)
    {
        var results = await ReadNodeAttributesBatchAsync([nodeId], ct).ConfigureAwait(false);
        return results[0];
    }

    public async Task<NodeAttributes[]> ReadNodeAttributesBatchAsync(string[] nodeIds, CancellationToken ct = default)
    {
        const int AttributesPerNode = 7;
        try
        {
            if (_session == null || _session.Connected == false)
                return nodeIds.Select(id => new NodeAttributes(id, id, null, "Unknown", 0, 0, false, false, ValueRanks.Scalar, null, StatusCodes.BadNotConnected, "Session is not connected")).ToArray();

            var nodesToRead = new ReadValueIdCollection(nodeIds.SelectMany(id => new[]
            {
                new ReadValueId { NodeId = id, AttributeId = Attributes.DisplayName },
                new ReadValueId { NodeId = id, AttributeId = Attributes.DataType },
                new ReadValueId { NodeId = id, AttributeId = Attributes.AccessLevel },
                new ReadValueId { NodeId = id, AttributeId = Attributes.UserAccessLevel },
                new ReadValueId { NodeId = id, AttributeId = Attributes.ValueRank },
                new ReadValueId { NodeId = id, AttributeId = Attributes.Description },
                new ReadValueId { NodeId = id, AttributeId = Attributes.Value },
            }));

            var response = await _session.ReadAsync(null, 0, TimestampsToReturn.Neither, nodesToRead, ct).ConfigureAwait(false);
            ClientBase.ValidateResponse(response.Results, nodesToRead);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, nodesToRead);
            var results = response.Results;

            return nodeIds.Select((id, i) =>
            {
                var offset = i * AttributesPerNode;
                var displayName = StatusCode.IsGood(results[offset].StatusCode) ? results[offset].Value as LocalizedText : null;
                var dataTypeNodeId = StatusCode.IsGood(results[offset + 1].StatusCode) ? results[offset + 1].Value as NodeId : null;
                var accessLevel = StatusCode.IsGood(results[offset + 2].StatusCode) ? Convert.ToByte(results[offset + 2].Value) : (byte)0;
                var userAccessLevel = StatusCode.IsGood(results[offset + 3].StatusCode) ? Convert.ToByte(results[offset + 3].Value) : (byte)0;
                var valueRank = StatusCode.IsGood(results[offset + 4].StatusCode) ? Convert.ToInt32(results[offset + 4].Value) : ValueRanks.Scalar;
                var description = StatusCode.IsGood(results[offset + 5].StatusCode) ? results[offset + 5].Value as LocalizedText : null;
                var value = StatusCode.IsGood(results[offset + 6].StatusCode) ? results[offset + 6].Value : null;
                var dataTypeName = dataTypeNodeId?.ToString() ?? "Unknown";
                return new NodeAttributes(
                    NodeId: id,
                    DisplayName: displayName?.Text ?? id,
                    Value: value,
                    DataType: dataTypeName,
                    AccessLevel: accessLevel,
                    UserAccessLevel: userAccessLevel,
                    CanWrite: (userAccessLevel & AccessLevels.CurrentWrite) != 0,
                    CanRead: (userAccessLevel & AccessLevels.CurrentRead) != 0,
                    ValueRank: valueRank,
                    Description: description?.Text
                );
            }).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReadNodeAttributesBatchAsync failed for {Count} nodes", nodeIds.Length);
            return nodeIds.Select(id => new NodeAttributes(id, id, null, "Unknown", 0, 0, false, false, ValueRanks.Scalar, null, StatusCodes.Bad, ex.Message)).ToArray();
        }
    }

    public async Task<WriteResult> WriteValueAsync(WriteValue value, CancellationToken ct = default)
    {
        var results = await WriteValuesAsync([value], ct).ConfigureAwait(false);
        return results[0];
    }

    public async Task<WriteResult[]> WriteValuesAsync(WriteValue[] values, CancellationToken ct = default)
    {
        try
        {
            if (values == null || values.Length == 0)
                return [];
            if (_session == null || _session.Connected == false)
                return values.Select(v => new WriteResult(v.NodeId, StatusCodes.BadNotConnected, "Session is not connected")).ToArray();

            // Build WriteValueCollection
            var nodesToWrite = new WriteValueCollection();
            foreach (var value in values)
            {
                var nodeId = NodeId.Parse(value.NodeId);

                object convertedValue;
                if (value.TargetTypeNodeId != null)
                {
                    var targetTypeNodeId = NodeId.Parse(value.TargetTypeNodeId);
                    var targetType = TypeInfo.GetSystemType(targetTypeNodeId, _session.Factory);
                    convertedValue = ConvertToOpcUaType(value.Value, targetType);
                }
                else
                {
                    // No type conversion - use value as-is
                    convertedValue = value.Value;
                }

                var writeValue = new Opc.Ua.WriteValue
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value,
                    Value = new Opc.Ua.DataValue { Value = convertedValue }
                };
                nodesToWrite.Add(writeValue);
            }

            // Execute batch write
            var response = await _session.WriteAsync(null, nodesToWrite, ct).ConfigureAwait(false);
            ClientBase.ValidateResponse(response.Results, nodesToWrite);
            ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, nodesToWrite);

            return values.Zip(response.Results, (v, r) =>
                new WriteResult(v.NodeId, r.Code, StatusCode.IsGood(r) ? null : r.ToString())
            ).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WriteValuesAsync failed for {Count} nodes", values?.Length ?? 0);
            return values?.Select(v => new WriteResult(v.NodeId, StatusCodes.Bad, ex.Message)).ToArray() ?? [];
        }
    }

    // TODO: brug lidt tid på at forstå og teste den her... 
    private static object ConvertToOpcUaType(object value, Type targetType)
    {
        // Extract actual value from JsonElement if needed
        object actualValue = value;
        if (value is JsonElement jsonElement)
        {
            actualValue = jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString()!,
                // Prefer Int64 — extracting every number as double would corrupt integer values above 2^53 before ChangeType runs
                JsonValueKind.Number => jsonElement.TryGetInt64(out var i64) ? i64 : (object)jsonElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => throw new ArgumentException("Cannot write null value"),
                _ => throw new ArgumentException($"Unsupported JSON value kind: {jsonElement.ValueKind}")
            };
        }

        // Handle Variant type - just return the value as-is
        if (targetType == null || targetType == typeof(Variant) || targetType == typeof(object))
            return actualValue;

        // Use Convert.ChangeType for built-in types
        try
        {
            // Invariant culture: string values like "1.5" must parse the same on every host — CurrentCulture would expect "1,5" on e.g. da-DK
            return Convert.ChangeType(actualValue, targetType, CultureInfo.InvariantCulture);
        }
        catch (InvalidCastException) when (!typeof(IConvertible).IsAssignableFrom(targetType))
        {
            // Convert.ChangeType only works against IConvertible target types; targetType isn't one, so there's no conversion to perform - use the value as-is.
            return actualValue;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Cannot convert value '{actualValue}' to type '{targetType.Name}': {ex.Message}", ex);
        }
    }

}
