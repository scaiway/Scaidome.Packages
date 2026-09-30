namespace Scaidome;

/// <summary>How a kind is held. Epoch, colour and matrix borrow the storage of another kind.</summary>
public enum VariantStorage
{
    None,
    Number,
    Text,
    DateTime,
}
