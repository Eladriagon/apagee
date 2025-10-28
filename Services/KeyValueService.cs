namespace Apagee.Services;

public class KeyValueService(StorageService storageService)
{
    public StorageService StorageService { get; } = storageService;

    public async Task<string?> Get(string key)
    {
        using var conn = await StorageService.Conn();

        return await conn.QueryFirstOrDefaultAsync<string>("SELECT Value FROM KeyValueStore WHERE Key = @key", new { key });
    }

    public async Task<bool> Set(string key, string value)
    {
        using var conn = await StorageService.Conn();

        return await conn.ExecuteAsync("INSERT OR REPLACE INTO KeyValueStore (Key, Value) VALUES (@key, @value)", new { key, value }) >= 1;
    }

    public async Task<bool> Delete(string key)
    {
        using var conn = await StorageService.Conn();

        return await conn.ExecuteAsync("DELETE FROM KeyValueStore WHERE Key = @key", new { key }) >= 1;
    }
}