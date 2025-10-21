namespace Apagee.Services;

public sealed class UserService(StorageService storage, SecurityHelper security)
{
    public StorageService Storage { get; } = storage;
    public SecurityHelper Security { get; } = security;

    /// <summary>
    /// Gets the only user in the User table, <see langword="null" /> if no users exist, or throws an exception.
    /// </summary>
    /// <exception cref="ApageeException">Thrown when the record count > 1.</exception>
    /// <exception cref="SqliteException">Thrown if there is a SQL or ORM problem.</exception>
    public async Task<User?> GetUser()
    {
        using var conn = await Storage.Conn();

        var recordCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM User");
        if (recordCount > 1)
        {
            throw new ApageeException($"Table [User] has unexpected row count - expected 1, found {recordCount}.");
        }

        return (await conn.GetAllAsync<User>()).FirstOrDefault();
    }

    public async Task<bool> CheckUserLogin(string username, string providedPass)
    {
        var user = await GetUser();

        if (user is null)
        {
            throw new ApageeException("No user found in database, unable to login.");
        }
        else
        {
            return username.IEquals(user.Username) && user.PassHash == Security.Hash(providedPass, Security.Encrypt(user.Uid));
        }
    }

    public async Task UpsertUser(User user, string? newPass = null)
    {
        using var conn = await Storage.Conn();

        var existing = await GetUser();

        if (existing == null)
        {
            if (newPass is not { Length: >= 8 })
            {
                throw new ApageeException("New password must be at least 8 characters.");
            }

            user.Uid = Guid.NewGuid().ToString();
            user.PassHash = Security.Hash(newPass, Security.Encrypt(user.Uid));

            await conn.InsertAsync(user);
        }
        else
        {
            if (newPass is { Length: >= 8 })
            {
                user.PassHash = Security.Hash(newPass, Security.Encrypt(user.Uid));
            }
            await conn.UpdateAsync(user);
        }
    }
}