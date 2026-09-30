using System.Text.Json;

namespace Scaidome.Json;

/// <summary>
/// The conversion error of the JSON rules. It names the kind that was being read and carries the document, cut to its first
/// 500 characters so a large document cannot flood a log.
/// </summary>
public sealed class JsonDocumentException : JsonException
{
    public const int DocumentLimit = 500;

    public JsonDocumentException(string kindName, string? document, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        KindName = kindName;
        Document = Cut(document);
    }

    /// <summary>The settings kind, or the stored name, that was being read.</summary>
    public string KindName { get; }

    /// <summary>The document, cut to its first 500 characters; null when there was none.</summary>
    public string? Document { get; }

    public static JsonDocumentException ForKind(Type kind, string? document, Exception? innerException = null) =>
        new(
            kind.Name,
            document,
            $"The document could not be read as {kind.Name}: {(document is null ? "no document" : Cut(document))}",
            innerException);

    private static string? Cut(string? document) =>
        document is { Length: > DocumentLimit } ? document[..DocumentLimit] : document;
}
