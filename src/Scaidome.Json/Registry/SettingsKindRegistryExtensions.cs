namespace Scaidome.Json;

/// <summary>Reads a typed document under its stored name.</summary>
public static class SettingsKindRegistryExtensions
{
    /// <summary>
    /// Resolves the stored name to its kind and reads the document as that kind. An unregistered name, a document that is not
    /// valid for the kind, and a null document all fail with <see cref="JsonDocumentException"/>.
    /// </summary>
    public static object ReadStored(this IJsonDocumentReader reader, ISettingsKindRegistry registry, string storedName, string? document)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(registry);
        return reader.Read(document, registry.GetKind(storedName));
    }

    public static T ReadStored<T>(this IJsonDocumentReader reader, ISettingsKindRegistry registry, string storedName, string? document)
        where T : class
    {
        var result = reader.ReadStored(registry, storedName, document);
        return result as T
            ?? throw new JsonDocumentException(
                storedName,
                document,
                $"The stored name '{storedName}' names {result.GetType().Name}, which is not a {typeof(T).Name}.");
    }
}
