namespace Scaidome.Json;

/// <summary>One settings kind registered under a stored name.</summary>
public sealed record SettingsKindRegistration
{
    public SettingsKindRegistration(string storedName, Type kind)
    {
        StoredName = storedName;
        Kind = kind;
    }

    public string StoredName { get; }

    public Type Kind { get; }
}
