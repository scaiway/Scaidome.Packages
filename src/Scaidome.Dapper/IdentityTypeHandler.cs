using System.Data;
using Dapper;

namespace Scaidome.Dapper;

/// <summary>
/// Reads an identity that SQLite holds as text. Any case is accepted, and text that is not an identity is an error, never a
/// default identity: a silently empty identity would point a row at nothing.
/// </summary>
public sealed class IdentityTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value) => value switch
    {
        Guid guid => guid,
        string text => Guid.TryParse(text, out var parsed)
            ? parsed
            : throw new DataException($"Stored text '{text}' is not an identity."),
        byte[] { Length: 16 } bytes => new Guid(bytes),
        _ => throw new DataException($"A stored {value.GetType().Name} is not an identity."),
    };

    /// <summary>
    /// The rules only read. The parameter keeps the identity itself, so the database driver binds it in its own form, which is
    /// what every store holds.
    /// </summary>
    public override void SetValue(IDbDataParameter parameter, Guid value) => parameter.Value = value;
}
