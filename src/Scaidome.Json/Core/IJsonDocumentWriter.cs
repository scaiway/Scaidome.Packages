namespace Scaidome.Json;

/// <summary>The writing half of the one codec for typed documents.</summary>
public interface IJsonDocumentWriter
{
    /// <summary>
    /// Writes settings from their own runtime kind, never from a more general one, so every field of that kind is kept. The
    /// document carries no kind field: the stored name kept beside it decides the kind.
    /// </summary>
    string Write(object settings);
}
