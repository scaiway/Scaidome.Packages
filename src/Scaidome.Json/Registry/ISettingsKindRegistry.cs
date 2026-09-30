using System.Diagnostics.CodeAnalysis;

namespace Scaidome.Json;

/// <summary>
/// The registry of stored names: the name kept beside a typed document that says which settings kind it holds. Stored names
/// are unique without regard to case; registering one twice is an error that stops the host from starting.
/// </summary>
public interface ISettingsKindRegistry
{
    /// <summary>Registers a kind under a stored name and returns the registry, so registrations chain.</summary>
    ISettingsKindRegistry Register(string storedName, Type kind);

    ISettingsKindRegistry Register<TKind>(string storedName)
        where TKind : class;

    /// <summary>The kind registered under a stored name. Fails with <see cref="JsonDocumentException"/>, listing the registered names.</summary>
    Type GetKind(string storedName);

    bool TryGetKind(string? storedName, [NotNullWhen(true)] out Type? kind);

    /// <summary>The stored name a kind is registered under. Fails when the kind is not registered.</summary>
    string GetStoredName(Type kind);

    bool TryGetStoredName(Type kind, [NotNullWhen(true)] out string? storedName);

    /// <summary>Every registration, in the order registered.</summary>
    IReadOnlyList<SettingsKindRegistration> ListRegistrations();
}
