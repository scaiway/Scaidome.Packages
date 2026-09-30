using System.Diagnostics.CodeAnalysis;

namespace Scaidome.Json;

/// <summary>
/// The registry of stored names. Registrations happen while a host starts and reads happen on every request, so the registry
/// locks its writes and copies on write; readers never take the lock.
/// </summary>
public sealed class SettingsKindRegistry : ISettingsKindRegistry
{
    private readonly Lock _lock = new();
    private volatile State _state = new([], new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase), []);

    public ISettingsKindRegistry Register(string storedName, Type kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storedName);
        ArgumentNullException.ThrowIfNull(kind);
        lock (_lock)
        {
            var state = _state;
            if (state.ByName.TryGetValue(storedName, out var existing))
            {
                throw new InvalidOperationException(
                    $"The stored name '{storedName}' is already registered for {existing.FullName}; stored names are unique without regard to case.");
            }

            if (state.ByKind.TryGetValue(kind, out var existingName))
            {
                throw new InvalidOperationException(
                    $"The settings kind {kind.FullName} is already registered under the stored name '{existingName}'.");
            }

            var byName = new Dictionary<string, Type>(state.ByName, StringComparer.OrdinalIgnoreCase) { [storedName] = kind };
            var byKind = new Dictionary<Type, string>(state.ByKind) { [kind] = storedName };
            _state = new State([.. state.Registrations, new SettingsKindRegistration(storedName, kind)], byName, byKind);
        }

        return this;
    }

    public ISettingsKindRegistry Register<TKind>(string storedName)
        where TKind : class =>
        Register(storedName, typeof(TKind));

    public Type GetKind(string storedName)
    {
        if (TryGetKind(storedName, out var kind))
        {
            return kind;
        }

        var registered = string.Join(", ", _state.Registrations.Select(registration => registration.StoredName));
        throw new JsonDocumentException(
            storedName ?? string.Empty,
            null,
            $"The stored name '{storedName}' is not registered. Registered names: {(registered.Length == 0 ? "none" : registered)}.");
    }

    public bool TryGetKind(string? storedName, [NotNullWhen(true)] out Type? kind)
    {
        kind = null;
        return storedName is not null && _state.ByName.TryGetValue(storedName, out kind);
    }

    public string GetStoredName(Type kind) =>
        TryGetStoredName(kind, out var storedName)
            ? storedName
            : throw new InvalidOperationException($"The settings kind {kind.FullName} has no stored name.");

    public bool TryGetStoredName(Type kind, [NotNullWhen(true)] out string? storedName)
    {
        ArgumentNullException.ThrowIfNull(kind);
        return _state.ByKind.TryGetValue(kind, out storedName);
    }

    public IReadOnlyList<SettingsKindRegistration> ListRegistrations() => _state.Registrations;

    private sealed record State(
        SettingsKindRegistration[] Registrations,
        Dictionary<string, Type> ByName,
        Dictionary<Type, string> ByKind);
}
