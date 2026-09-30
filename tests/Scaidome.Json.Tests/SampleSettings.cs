namespace Scaidome.Json.Tests;

/// <summary>A general settings kind with specific kinds, as the platform declares them.</summary>
public abstract record SampleSettings
{
    protected SampleSettings(string? label)
    {
        Label = label ?? string.Empty;
    }

    public string Label { get; }
}

public sealed record PumpSettings : SampleSettings
{
    public PumpSettings(string? label = null, int maxSpeed = 1500, SampleMode mode = SampleMode.Auto, bool isReversible = false)
        : base(label)
    {
        MaxSpeed = maxSpeed;
        Mode = mode;
        IsReversible = isReversible;
    }

    public int MaxSpeed { get; }

    public SampleMode Mode { get; }

    public bool IsReversible { get; }
}

public sealed record ValveSettings : SampleSettings
{
    public ValveSettings(string? label = null, double openTime = 2.5)
        : base(label)
    {
        if (openTime < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(openTime));
        }

        OpenTime = openTime;
    }

    public double OpenTime { get; }
}
