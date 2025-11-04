namespace Apagee.Services;

public class InteractionService(StorageService storageService)
{
    public StorageService StorageService { get; } = storageService;

    public async Task<bool> InteractionExists(Interaction interaction)
    {
        using var conn = await StorageService.Conn();

        return await conn.ExecuteScalarAsync<bool>("SELECT 1 FROM Interaction WHERE ID = @id", new { id = interaction.ID });
    }

    public async Task CreateInteraction(Interaction interaction)
    {
        using var conn = await StorageService.Conn();

        await conn.InsertAsync(interaction);
    }

    public async Task<Dictionary<InteractionType, uint>> GetInteractionCounts(Article article)
        => await GetInteractionCounts(article.Uid);

    public async Task<Dictionary<InteractionType, uint>> GetInteractionCounts(string uid)
    {
        using var conn = await StorageService.Conn();

        return new() {
            [InteractionType.Like] = await conn.ExecuteScalarAsync<uint>("SELECT COUNT(*) FROM Interaction WHERE ArticleUID = @uid AND Type = @type", new { uid, InteractionType.Like }),
            [InteractionType.Announce] = await conn.ExecuteScalarAsync<uint>("SELECT COUNT(*) FROM Interaction WHERE ArticleUID = @uid AND Type = @type", new { uid, InteractionType.Announce })
        };
    }

    public async Task<uint> GetInteractionCount(Article article, InteractionType type)
        => await GetInteractionCount(article.Uid, type);

    public async Task<uint> GetInteractionCount(string uid, InteractionType type)
    {
        using var conn = await StorageService.Conn();

        return await conn.ExecuteScalarAsync<uint>("SELECT COUNT(*) FROM Interaction WHERE ArticleUID = @uid AND Type = @type", new { uid, type });
    }

    public async Task<IEnumerable<Interaction>> GetAllInteractions()
    {
        using var conn = await StorageService.Conn();

        return await conn.QueryAsync<Interaction>("SELECT * FROM Interaction");
    }

    public async Task<bool> DeleteInteraction(Interaction interaction)
        => await DeleteInteraction(interaction.ID);

    public async Task<bool> DeleteInteraction(string id)
    {
        using var conn = await StorageService.Conn();

        return await conn.ExecuteAsync("DELETE FROM Interaction WHERE ID = @id", new { id }) == 1;
    }
}