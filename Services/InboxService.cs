namespace Apagee.Services;

public class InboxService(StorageService storageService)
{
    public StorageService StorageService { get; } = storageService;

    public async Task Create(Inbox item)
    {
        using var conn = await StorageService.Conn();

        await conn.InsertAsync(item);
    }

    public async Task<bool> CreateFollower(APubFollower follower)
    {
        using var conn = await StorageService.Conn();

        if (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM APubFollowers WHERE ID = @id OR FollowerId = @followerId;", new { id = follower.Id, followerId = follower.FollowerId }) > 0)
        {
            // Follower already exists, duplicate.
            return false;
        }
        else
        {
            await conn.InsertAsync(follower);
            return true;
        }
    }

    public async Task<bool> DeleteFollower(string followerId)
    {
        using var conn = await StorageService.Conn();

        return await conn.ExecuteAsync("DELETE FROM APubFollowers WHERE ID = @id", new { id = followerId }) == 1;
    }
    
    public async Task<uint> GetFollowerCount(string? domain = null)
    {
        using var conn = await StorageService.Conn();

        return await conn.ExecuteScalarAsync<uint>($"SELECT COUNT(*) FROM APubFollowers {(domain is string { Length: > 0 } d ? $"WHERE FollowerId LIKE '%{d}%'" : "")};");
    }
    
    public async Task<IEnumerable<string>> GetFollowerList(string? olderThan = null, string? domain = null, int count = 100)
    {
        using var conn = await StorageService.Conn();

        if (count is < 1 or > 200 || (olderThan is not null && !Ulid.TryParse(olderThan, out var _)))
        {
            throw new ApageeException($"Validation error on follower list. olderThan={olderThan ?? "NULL"}, domain={domain ?? "NULL"}, count={count}");
        }

        var where = "";
        where += olderThan is string { Length: > 0 } ot
            ? $" UID < '{ot}'"
            : "";

        where += domain is string { Length: > 0 } d
            ? $" {(where is { Length: > 0 } ? "AND " : "" )}FollowerId LIKE '%{d}%'"
            : "";

        return await conn.QueryAsync<string>($"SELECT FollowerId FROM APubFollowers {(where is {  Length: > 0 } ? "WHERE" + where : "")} ORDER BY UID DESC LIMIT {count};");
    }
}