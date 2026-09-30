namespace Scaidome.Abstractions;

/// <summary>One observed measurement of a value type, with named tags.</summary>
public readonly struct Measurement<T>
    where T : struct
{
    public Measurement(T value, params KeyValuePair<string, object?>[] tags)
    {
        Value = value;
        Tags = tags;
    }

    public T Value { get; }

    public IReadOnlyList<KeyValuePair<string, object?>> Tags { get; }
}
