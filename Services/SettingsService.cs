namespace Apagee.Services;

public class SettingsService(StorageService storageService)
{
    public StorageService StorageService { get; } = storageService;

    public static Settings? Current { get; private set; }

    public async Task RefreshSettings()
    {
        try
        {
            using var conn = await StorageService.Conn();

            var settings = await conn.QueryFirstOrDefaultAsync<Settings>($"SELECT * FROM Settings LIMIT 1");

            Current = settings;
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Can't fetch settings - {ex.Message}", ex);
        }
    }
    
    public async Task UpsertSettings(Settings newSettings)
    {
        try
        {
            using var conn = await StorageService.Conn();
            await RefreshSettings();

            if (Current is null)
            {
                await conn.InsertAsync(newSettings);
            }
            else
            {
                await conn.UpdateAsync(newSettings);
            }
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Can't fetch settings - {ex.Message}", ex);
        }
    }
}