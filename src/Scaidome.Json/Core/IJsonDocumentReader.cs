namespace Scaidome.Json;

/// <summary>The reading half of the one codec for typed documents.</summary>
public interface IJsonDocumentReader
{
    /// <summary>
    /// Reads a document as the given settings kind. Fails with <see cref="JsonDocumentException"/> when the document is not
    /// valid JSON for the kind, or is null.
    /// </summary>
    object Read(string? document, Type kind);

    T Read<T>(string? document)
        where T : class;
}
