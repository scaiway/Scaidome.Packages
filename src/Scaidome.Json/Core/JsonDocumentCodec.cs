using System.Text.Json;

namespace Scaidome.Json;

/// <summary>
/// The one codec that both reads and writes typed documents, so the two directions cannot drift apart. Every reader and writer
/// of a typed document uses the same rules: rules that differ between two writers would spell the same field two ways.
/// </summary>
public sealed class JsonDocumentCodec : IJsonDocumentReader, IJsonDocumentWriter
{
    public JsonDocumentCodec(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.MakeReadOnly(populateMissingResolver: true);
        Options = options;
    }

    /// <summary>The serializer options the codec applies. Read-only.</summary>
    public JsonSerializerOptions Options { get; }

    public object Read(string? document, Type kind)
    {
        ArgumentNullException.ThrowIfNull(kind);
        if (document is null)
        {
            throw JsonDocumentException.ForKind(kind, document);
        }

        object? result;
        try
        {
            result = JsonSerializer.Deserialize(document, kind, Options);
        }
        catch (JsonException ex)
        {
            throw JsonDocumentException.ForKind(kind, document, ex);
        }
        catch (NotSupportedException ex)
        {
            throw JsonDocumentException.ForKind(kind, document, ex);
        }
        catch (ArgumentException ex)
        {
            // A settings constructor that refuses a value is a document that is not valid for its kind.
            throw JsonDocumentException.ForKind(kind, document, ex);
        }

        // The literal null is a document without settings, which no record may hold.
        return result ?? throw JsonDocumentException.ForKind(kind, document);
    }

    public T Read<T>(string? document)
        where T : class =>
        (T)Read(document, typeof(T));

    public string Write(object settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return JsonSerializer.Serialize(settings, settings.GetType(), Options);
    }
}
