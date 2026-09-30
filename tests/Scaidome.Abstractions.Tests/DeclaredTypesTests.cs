namespace Scaidome.Abstractions.Tests;

public class DeclaredTypesTests
{
    [Fact]
    public void The_nine_words_map_to_their_kinds()
    {
        DeclaredTypes.All.Should().Equal("int", "number", "boolean", "string", "json", "datetime", "epoch", "color", "matrix");
        var kinds = DeclaredTypes.All.Select(word => DeclaredTypes.TryGetKind(word, out var kind) ? kind : VariantKind.Null);
        kinds.Should().Equal(
            VariantKind.Int, VariantKind.Double, VariantKind.Bool, VariantKind.String, VariantKind.Json,
            VariantKind.DateTime, VariantKind.Epoch, VariantKind.Color, VariantKind.Matrix);
    }

    [Fact]
    public void Every_kind_except_no_value_has_a_type()
    {
        foreach (var kind in Enum.GetValues<VariantKind>().Where(kind => kind != VariantKind.Null))
        {
            DeclaredTypes.TryGetKind(DeclaredTypes.GetTypeWord(kind), out var back).Should().BeTrue();
            back.Should().Be(kind);
        }

        FluentActions.Invoking(() => DeclaredTypes.GetTypeWord(VariantKind.Null)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Words_match_without_regard_to_case_and_keep_lower_case()
    {
        DeclaredTypes.TryNormalize("DateTime", out var type).Should().BeTrue();
        type.Should().Be("datetime");
        DeclaredTypes.IsDeclaredType("float").Should().BeFalse();
        DeclaredTypes.IsDeclaredType(null).Should().BeFalse();
    }

    [Fact]
    public void A_stored_word_that_is_not_a_type_maps_to_text()
    {
        DeclaredTypes.GetStoredKind("float").Should().Be(VariantKind.String);
        DeclaredTypes.GetStoredKind(null).Should().Be(VariantKind.String);
        DeclaredTypes.GetStoredKind("NUMBER").Should().Be(VariantKind.Double);
    }
}
