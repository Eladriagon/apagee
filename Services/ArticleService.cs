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

            await DeleteTags(newArticle);
            if (newArticle.Tags is { Count: > 0 })
            {
                await AddTags(newArticle, newArticle.Tags);
            }

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

            var articles = await conn.QueryAsync<Article>($"SELECT * FROM Article WHERE Status = {(int)ArticleStatus.Published} AND UID {(inclusive ? ">=" : ">")} '{ulid ?? Ulid.MinValue.ToString()}' ORDER BY UID ASC LIMIT {count};");

            foreach (var article in articles)
            {
                article.Tags = await GetTagsForArticle(article);
            }

            return articles;
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

            var articles = await conn.QueryAsync<Article>($"SELECT * FROM Article WHERE Status = {(int)ArticleStatus.Published} AND UID {(inclusive ? "<=" : "<")} '{ulid ?? Ulid.MaxValue.ToString()}' ORDER BY UID DESC LIMIT {count};");

            foreach (var article in articles)
            {
                article.Tags = await GetTagsForArticle(article);
            }

            return articles;
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

            var article = await conn.QueryFirstOrDefaultAsync<Article>(query.Sql, query.Parameters);

            if (article is null) return null;

            article.Tags = await GetTagsForArticle(article);

            return article;
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

            var article = await conn.GetAsync<Article>(uid);

            if (article is null) return null;

            article.Tags = await GetTagsForArticle(article);

            return article;
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

            var articles = await conn.QueryAsync<Article>($"SELECT * FROM Article ORDER BY CreatedOn DESC LIMIT {count};");

            foreach (var article in articles)
            {
                article.Tags = await GetTagsForArticle(article);
            }

            return articles;
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

            var articles = await conn.QueryAsync<Article>(query.Sql, query.Parameters);
            
            foreach (var article in articles)
            {
                article.Tags = await GetTagsForArticle(article);
            }

            return articles;
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

            await DeleteTags(article);
            if (article.Tags is { Count: > 0 })
            {
                await AddTags(article, article.Tags);
            }
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

            await DeleteTags(article);
            await conn.DeleteAsync(article);
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to delete article '{article.Uid}' - {ex.Message}", ex);
        }
    }

    public async Task DeleteTags(Article article)
    {
        try
        {
            using var conn = await StorageService.Conn();

            await conn.ExecuteAsync("DELETE FROM Tags WHERE ArticleUID = @uid", new { uid = article.Uid });
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to delete tags for article '{article.Uid}' - {ex.Message}", ex);
        }
    }

    public async Task AddTags(Article article, IList<string> tags)
    {
        try
        {
            using var conn = await StorageService.Conn();

            foreach (var tag in tags)
            {
                await conn.ExecuteAsync("INSERT INTO Tags (ArticleUID, Tag) VALUES (@uid, @tag)", new { uid = article.Uid, tag });
            }
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to add tags for article '{article.Uid}' - {ex.Message}", ex);
        }
    }

    public async Task<IList<string>?> GetTagsForArticle(Article article)
    {
        try
        {
            using var conn = await StorageService.Conn();

            return (await conn.QueryAsync<string>("SELECT Tag FROM Tags WHERE ArticleUID = @uid", new { uid = article.Uid }))?.ToList();
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to add tags for article '{article.Uid}' - {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<Article>?> GetArticlesByTag(string tag, bool includeAll = false)
    {
        if (tag is not { Length: > 0 }) return null;

        try
        {
            using var conn = await StorageService.Conn();

            var articleList = await conn.QueryAsync<string>("SELECT ArticleUID FROM Tags WHERE Tag = @tag", new { tag });

            var articles = new List<Article>();
            foreach (var uid in articleList)
            {
                var a = await GetByUid(uid);
                if (a is null || (!includeAll && a is not { Status: ArticleStatus.Published })) continue;
                articles.Add(a);
            }
            return articles.OrderByDescending(a => a.PublishedOn);
        }
        catch (Exception ex)
        {
            throw new ApageeException($"Failed to get article list for tag '{tag}' - {ex.Message}", ex);
        }
    }
}