using System.Data;
using System.Globalization;

namespace Scaidome.Dapper.Tests;

public class TimeTypeHandlerTests
{
    private readonly TimeTypeHandler _handler = new();

    [Theory]
    [InlineData("2026-09-03 14:22:07", 0)]
    [InlineData("2026-09-03 14:22:07.1234567", 1234567)]
    [InlineData("2026-09-03 14:22:07.123", 1230000)]
    [InlineData("2026-09-03T14:22:07", 0)]
    [InlineData("2026-09-03T14:22:07.1234567", 1234567)]
    [InlineData("2026-09-03T14:22:07Z", 0)]
    [InlineData("2026-09-03T14:22:07.123Z", 1230000)]
    [InlineData("2026-09-03T14:22:07.1234567Z", 1234567)]
    [InlineData("2026-09-03 14:22:07.12", 1200000)]
    public void The_listed_forms_read_as_utc(string text, long fractionTicks)
    {
        var expected = new DateTime(2026, 9, 3, 14, 22, 7, DateTimeKind.Utc).AddTicks(fractionTicks);

        var read = _handler.Parse(text);

        read.Should().Be(expected);
        read.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Forms_read_the_same_whatever_the_machine_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            _handler.Parse("2026-09-03 14:22:07.5").Should().Be(new DateTime(2026, 9, 3, 14, 22, 7, 500, DateTimeKind.Utc));
            CultureInfo.CurrentCulture = new CultureInfo("da-DK");
            _handler.Parse("2026-09-03T14:22:07Z").Should().Be(new DateTime(2026, 9, 3, 14, 22, 7, DateTimeKind.Utc));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Other_text_with_an_offset_is_converted_by_it()
    {
        var read = _handler.Parse("2026-09-03T16:22:07+02:00");

        read.Should().Be(new DateTime(2026, 9, 3, 14, 22, 7, DateTimeKind.Utc));
        read.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Other_invariant_date_text_reads_as_utc()
    {
        _handler.Parse("09/03/2026 14:22:07").Should().Be(new DateTime(2026, 9, 3, 14, 22, 7, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData("")]
    [InlineData("yesterday")]
    [InlineData("2026-13-45 99:99:99")]
    public void Text_that_is_not_a_time_is_an_error(string text)
    {
        FluentActions.Invoking(() => _handler.Parse(text)).Should().Throw<DataException>();
    }

    [Fact]
    public void A_native_time_is_read_without_text()
    {
        var unspecified = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Unspecified);
        _handler.Parse(unspecified).Should().Be(new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc));
        _handler.Parse(unspecified).Kind.Should().Be(DateTimeKind.Utc);

        var local = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Local);
        _handler.Parse(local).Should().Be(local.ToUniversalTime());

        var offset = new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.FromHours(-5));
        _handler.Parse(offset).Should().Be(new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void The_rules_write_utc_with_milliseconds_and_z()
    {
        var local = new DateTime(2026, 9, 3, 14, 22, 7, 123, DateTimeKind.Utc).AddTicks(4567);

        TimeTypeHandler.Format(local).Should().Be("2026-09-03T14:22:07.123Z");
    }
}
