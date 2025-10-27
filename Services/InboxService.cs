namespace Apagee.Services;

public class InboxService(StorageService storageService)
{
    public StorageService StorageService { get; } = storageService;

    public async Task Create(Inbox item)
    {
        using var conn = await StorageService.Conn();

        await conn.InsertAsync(item);
    }
}