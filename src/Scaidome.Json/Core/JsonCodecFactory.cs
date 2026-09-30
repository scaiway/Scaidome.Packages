using System.Text.Json;

namespace Scaidome.Json;

/// <summary>
/// The one factory that chooses the rules for typed documents: camel-case field names, the same names the API uses, read back
/// without regard to case. Enumerated settings stay numbers and every other option stays at the serializer's default. A caller
/// without a container takes its codec from here.
/// </summary>
public static class JsonCodecFactory
{
    private static readonly Lazy<JsonDocumentCodec> SharedCodec = new(Create);

    /// <summary>One codec shared by callers without a container.</summary>
    public static JsonDocumentCodec Shared => SharedCodec.Value;

    public static JsonCodecBuilder CreateBuilder() =>
        new JsonCodecBuilder()
            .WithCaseInsensitiveFields()
            .WithNamingPolicy(JsonNamingPolicy.CamelCase);

    public static JsonDocumentCodec Create() => CreateBuilder().Build();
}
