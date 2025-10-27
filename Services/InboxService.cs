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
    
    public async Task<uint> GetFollowerCount()
    {
        using var conn = await StorageService.Conn();

        return await conn.ExecuteScalarAsync<uint>("SELECT COUNT(*) FROM APubFollowers;");
    }
    
    public async Task<IEnumerable<string>> GetFollowerList(string? olderThan = null, int count = 100)
    {
        using var conn = await StorageService.Conn();

        if (count is < 1 or > 200 || !Ulid.TryParse(olderThan, out var _))
        {
            throw new ApageeException("Validation error on follower list.");
        }

        return await conn.QueryAsync<string>($"SELECT FollowerId FROM APubFollowers {(olderThan is string {Length: > 0 } ot ? $"WHERE UID < '{ot}'" : "")} ORDER BY UID DESC LIMIT {count};");
    }
}