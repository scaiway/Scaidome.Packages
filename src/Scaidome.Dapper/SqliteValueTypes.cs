using Dapper;

namespace Scaidome.Dapper;

/// <summary>
/// Registers the reading rules for identities and times. The data access library keeps its handlers per process, so the rules
/// apply to every SQLite store in the process; every part that opens one registers them before its first connection, and
/// whichever part registers last reads every store. Registering again changes nothing.
/// </summary>
public static class SqliteValueTypes
{
    private static readonly IdentityTypeHandler Identity = new();
    private static readonly TimeTypeHandler Time = new();
    private static readonly Lock RegistrationLock = new();

    public static void Register()
    {
        lock (RegistrationLock)
        {
            SqlMapper.AddTypeHandler(Identity);
            SqlMapper.AddTypeHandler(Time);

            // Readers compiled before the handlers existed would keep reading without them.
            SqlMapper.PurgeQueryCache();
        }
    }
}
