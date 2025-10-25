using static Dapper.SimpleSqlBuilder.SimpleBuilder;

namespace Apagee.Services;

public class ArticleService(StorageService storageService)
{
    public StorageService StorageService { get; } = storageService;

    public async Task<Article> Create(Article newArticle)
    {
        try
        {
            using var conn = await StorageService.Conn();

            await conn.InsertAsync(newArticle);
            return await conn.GetAsync<Article>(newArticle.Uid);
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to create article - {ex.Message}", ex);
        }
    }

    public async Task<uint> GetCount(bool publishedOnly = false)
    {
        try
        {
            using var conn = await StorageService.Conn();

            return await conn.ExecuteScalarAsync<uint>($"SELECT COUNT(*) FROM Article {(publishedOnly ? $"WHERE Status = {(int)ArticleStatus.Published}" : "")}");
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to get article count - {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Returns an oldest-to-newest list of articles newer than the specified ulid.
    /// </summary>
    public async Task<IEnumerable<Article>> GetNewerThan(string? ulid = null, bool inclusive = false, int count = 25)
    {
        try
        {
            using var conn = await StorageService.Conn();

            return await conn.QueryAsync<Article>($"SELECT * FROM Article WHERE Status = {(int)ArticleStatus.Published} AND UID {(inclusive ? ">=" : ">")} '{ulid ?? Ulid.MinValue.ToString()}' ORDER BY UID ASC LIMIT {count};");
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to get articles - {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Returns a newest-to-oldest list of articles newer than the specified ulid.
    /// </summary>
    public async Task<IEnumerable<Article>> GetOlderThan(string? ulid = null, bool inclusive = false, int count = 25)
    {
        try
        {
            using var conn = await StorageService.Conn();

            return await conn.QueryAsync<Article>($"SELECT * FROM Article WHERE Status = {(int)ArticleStatus.Published} AND UID {(inclusive ? "<=" : "<")} '{ulid ?? Ulid.MaxValue.ToString()}' ORDER BY UID DESC LIMIT {count};");
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to get articles - {ex.Message}", ex);
        }
    }

    public async Task<Article?> GetBySlug(string slug)
    {
        try
        {
            using var conn = await StorageService.Conn();

            var query = CreateFluent()
                .Select($"*")
                .From($"Article")
                .Where($"Slug = {slug}");

            return await conn.QueryFirstOrDefaultAsync<Article>(query.Sql, query.Parameters);
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to get article by slug '{slug}' - {ex.Message}", ex);
        }
    }

    public async Task<Article?> GetByUid(string uid)
    {
        try
        {
            using var conn = await StorageService.Conn();

            return await conn.GetAsync<Article>(uid);
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to get article by UID '{uid}' - {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Article>> GetRecent(int count = 5)
    {
        try
        {
            using var conn = await StorageService.Conn();

            return await conn.QueryAsync<Article>($"SELECT * FROM Article ORDER BY CreatedOn DESC LIMIT {count};");
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to get all articles - {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Article>> GetAll()
    {
        try
        {
            using var conn = await StorageService.Conn();

            var query = CreateFluent()
                .Select($"*")
                .From($"Article");

            return await conn.QueryAsync<Article>(query.Sql, query.Parameters);
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to get all articles - {ex.Message}", ex);
        }
    }

    public async Task Update(Article article)
    {
        try
        {
            using var conn = await StorageService.Conn();

            await conn.UpdateAsync(article);
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to update article '{article.Uid}' - {ex.Message}", ex);
        }
    }

    public async Task Delete(Article article)
    {
        try
        {
            using var conn = await StorageService.Conn();

            await conn.DeleteAsync(article);
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to delete article '{article.Uid}' - {ex.Message}", ex);
        }
    }
}