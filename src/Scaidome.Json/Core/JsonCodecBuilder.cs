using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scaidome.Json;

/// <summary>Assembles a codec from switches. <see cref="JsonCodecFactory"/> is the one place that chooses them.</summary>
public sealed class JsonCodecBuilder
{
    private readonly List<JsonConverter> _converters = [];
    private bool _caseInsensitive;
    private bool _trailingCommas;
    private bool _comments;
    private JsonNamingPolicy? _namingPolicy;

    public JsonCodecBuilder WithCaseInsensitiveFields(bool enabled = true)
    {
        _caseInsensitive = enabled;
        return this;
    }

    public JsonCodecBuilder WithTrailingCommas(bool enabled = true)
    {
        _trailingCommas = enabled;
        return this;
    }

    public JsonCodecBuilder WithComments(bool enabled = true)
    {
        _comments = enabled;
        return this;
    }

    public JsonCodecBuilder WithNamingPolicy(JsonNamingPolicy? policy)
    {
        _namingPolicy = policy;
        return this;
    }

    public JsonCodecBuilder AddConverter(JsonConverter converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        _converters.Add(converter);
        return this;
    }

    public JsonSerializerOptions BuildOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = _caseInsensitive,
            AllowTrailingCommas = _trailingCommas,
            ReadCommentHandling = _comments ? JsonCommentHandling.Skip : JsonCommentHandling.Disallow,
            PropertyNamingPolicy = _namingPolicy,
        };
        foreach (var converter in _converters)
        {
            options.Converters.Add(converter);
        }

        return options;
    }

    public JsonDocumentCodec Build() => new(BuildOptions());
}
