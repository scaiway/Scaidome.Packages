using Dapper;
using Microsoft.Data.Sqlite;

namespace Scaidome.Dapper.Tests;

public class SqliteValueTypesTests
{
    private sealed class Row
    {
        public Guid Id { get; init; }

        public DateTime At { get; init; }

        public Guid? Other { get; init; }

        public DateTime? Maybe { get; init; }
    }

    [Fact]
    public void Registered_rules_read_what_the_driver_wrote()
    {
        SqliteValueTypes.Register();
        using var connection = Open();
        var id = Guid.NewGuid();
        var at = new DateTime(2026, 9, 3, 14, 22, 7, DateTimeKind.Utc).AddTicks(1234567);
        connection.Execute("CREATE TABLE T (Id TEXT COLLATE NOCASE, At TEXT, Other TEXT, Maybe TEXT)");
        connection.Execute("INSERT INTO T VALUES (@id, @at, NULL, NULL)", new { id, at });

        var stored = connection.QuerySingle<string>("SELECT Id FROM T");
        stored.Should().Be(id.ToString().ToUpperInvariant(), "the driver writes identities in upper case");
        connection.QuerySingle<string>("SELECT At FROM T").Should().Be("2026-09-03 14:22:07.1234567");

        var row = connection.QuerySingle<Row>("SELECT Id, At, Other, Maybe FROM T WHERE Id = @id", new { id });
        row.Id.Should().Be(id);
        row.At.Should().Be(at);
        row.At.Kind.Should().Be(DateTimeKind.Utc);
        row.Other.Should().BeNull();
        row.Maybe.Should().BeNull();
    }

    [Fact]
    public void Identity_columns_compare_without_regard_to_case()
    {
        SqliteValueTypes.Register();
        using var connection = Open();
        var id = Guid.NewGuid();
        connection.Execute("CREATE TABLE T (Id TEXT COLLATE NOCASE)");
        connection.Execute("INSERT INTO T VALUES (@text)", new { text = id.ToString().ToLowerInvariant() });

        connection.QuerySingle<Guid>("SELECT Id FROM T WHERE Id = @id", new { id }).Should().Be(id);
    }

    [Fact]
    public void Text_that_is_not_an_identity_fails_the_read()
    {
        SqliteValueTypes.Register();
        using var connection = Open();
        connection.Execute("CREATE TABLE T (Id TEXT, At TEXT)");
        connection.Execute("INSERT INTO T VALUES ('garbage', '2026-01-01 00:00:00')");

        FluentActions.Invoking(() => connection.Query<Row>("SELECT Id, At FROM T").ToList()).Should().Throw<Exception>();
    }

    [Fact]
    public void Registering_again_changes_nothing()
    {
        SqliteValueTypes.Register();
        SqliteValueTypes.Register();
        using var connection = Open();

        connection.QuerySingle<DateTime>("SELECT '2026-01-01T00:00:00Z'").Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Registering_reaches_readers_compiled_before_it()
    {
        SqlMapper.ResetTypeHandlers();
        try
        {
            using var connection = Open();
            const string sql = "SELECT '2026-01-01 08:00:00' AS At, '0f8fad5b-d9cb-469f-a165-70867728950e' AS Id";
            FluentActions.Invoking(() => connection.QuerySingle<Row>(sql))
                .Should().Throw<System.Data.DataException>("the library cannot read an identity from text on its own");

            SqliteValueTypes.Register();

            var after = connection.QuerySingle<Row>(sql);
            after.Id.Should().Be(Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"));
            after.At.Kind.Should().Be(DateTimeKind.Utc);
            after.At.Should().Be(new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc));
        }
        finally
        {
            SqliteValueTypes.Register();
        }
    }

    private static SqliteConnection Open()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }
}
