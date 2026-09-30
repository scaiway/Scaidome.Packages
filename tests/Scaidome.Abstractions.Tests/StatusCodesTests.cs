using System.Reflection;

namespace Scaidome.Abstractions.Tests;

public class StatusCodesTests
{
    [Theory]
    [InlineData(0x00000000u, true, false, false)]
    [InlineData(0x00960000u, true, false, false)]
    [InlineData(0x3FFFFFFFu, true, false, false)]
    [InlineData(0x40000000u, false, true, false)]
    [InlineData(0x40920000u, false, true, false)]
    [InlineData(0x80000000u, false, false, true)]
    [InlineData(0x803B0000u, false, false, true)]
    [InlineData(0xC0000000u, false, false, false)]
    public void The_top_two_bits_classify_a_code(uint code, bool good, bool uncertain, bool bad)
    {
        StatusCodes.IsGood(code).Should().Be(good);
        StatusCodes.IsUncertain(code).Should().Be(uncertain);
        StatusCodes.IsBad(code).Should().Be(bad);
        StatusCodes.IsReserved(code).Should().Be(!good && !uncertain && !bad);
    }

    [Fact]
    public void Every_named_code_is_classified_by_its_name()
    {
        var constants = typeof(StatusCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(uint))
            .ToList();

        constants.Should().HaveCountGreaterThan(100);
        foreach (var field in constants)
        {
            var code = (uint)field.GetRawConstantValue()!;
            if (field.Name.StartsWith("Good", StringComparison.Ordinal))
            {
                StatusCodes.IsGood(code).Should().BeTrue(field.Name);
            }
            else if (field.Name.StartsWith("Uncertain", StringComparison.Ordinal))
            {
                StatusCodes.IsUncertain(code).Should().BeTrue(field.Name);
            }
            else
            {
                StatusCodes.IsBad(code).Should().BeTrue(field.Name);
            }
        }

        constants.Select(field => field.GetRawConstantValue()).Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(StatusCodes.Good, "Good")]
    [InlineData(StatusCodes.Uncertain, "Uncertain")]
    [InlineData(StatusCodes.BadNodeIdUnknown, "BadNodeIdUnknown")]
    public void A_named_code_reads_as_its_name(uint code, string name)
    {
        StatusCodes.GetName(code).Should().Be(name);
    }

    [Fact]
    public void The_info_bits_do_not_change_a_codes_name()
    {
        StatusCodes.GetName(StatusCodes.BadNodeIdUnknown | 0x0000FFFF).Should().Be("BadNodeIdUnknown");
    }

    [Fact]
    public void An_unknown_code_reads_as_its_hex_value()
    {
        StatusCodes.GetName(0x80FF0000).Should().Be("0x80FF0000");
    }
}
