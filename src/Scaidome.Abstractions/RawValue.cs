namespace Scaidome;

/// <summary>
/// A value as a source read it, addressed to the tag it is for and not yet converted: the value is whatever the device handed
/// over, as a .NET type or null. The tag decides what it means.
/// </summary>
public record RawValue(Guid TagId, object? Value, uint StatusCode, DateTime SourceTimestamp, DateTime ServerTimestamp);
