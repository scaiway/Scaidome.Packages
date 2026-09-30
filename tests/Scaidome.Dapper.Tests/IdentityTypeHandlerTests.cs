using System.Data;

namespace Scaidome.Dapper.Tests;

public class IdentityTypeHandlerTests
{
    private readonly IdentityTypeHandler _handler = new();

    [Theory]
    [InlineData("0f8fad5b-d9cb-469f-a165-70867728950e")]
    [InlineData("0F8FAD5B-D9CB-469F-A165-70867728950E")]
    [InlineData("0F8fad5b-D9cb-469F-a165-70867728950E")]
    public void Standard_text_in_any_case_reads_as_the_identity(string text)
    {
        _handler.Parse(text).Should().Be(Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"));
    }

    [Fact]
    public void A_native_identity_passes_through()
    {
        var id = Guid.NewGuid();
        _handler.Parse(id).Should().Be(id);
        _handler.Parse(id.ToByteArray()).Should().Be(id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("0f8fad5b-d9cb-469f-a165")]
    public void Text_that_is_not_an_identity_is_an_error(string text)
    {
        FluentActions.Invoking(() => _handler.Parse(text)).Should().Throw<DataException>();
    }

    [Fact]
    public void Other_stored_types_are_an_error()
    {
        FluentActions.Invoking(() => _handler.Parse(42L)).Should().Throw<DataException>();
    }

    [Fact]
    public void Setting_a_parameter_leaves_the_identity_to_the_driver()
    {
        using var command = new Microsoft.Data.Sqlite.SqliteCommand();
        var parameter = command.CreateParameter();
        var id = Guid.NewGuid();

        _handler.SetValue(parameter, id);

        parameter.Value.Should().Be(id);
    }
}
