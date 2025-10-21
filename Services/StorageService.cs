namespace Apagee.Storage;

public class StorageService(GlobalConfiguration config, IWebHostEnvironment env)
{
    private GlobalConfiguration Config { get; } = config;
    public IWebHostEnvironment Env { get; } = env;

    public async Task<SqliteConnection> Conn()
    {
        var c = new SqliteConnection(Config.SqliteConnectionString);
        await c.OpenAsync();

        // Pragma defaults for SQLite.
        await c.ExecuteAsync("PRAGMA foreign_keys = ON;");
        await c.ExecuteAsync("PRAGMA temp_store = MEMORY;");
        await c.ExecuteAsync("PRAGMA busy_timeout = 5000;");

        return c;
    }

    public async Task<int> ExecuteAsync(string queryText)
    {
        using var conn = await Conn();

        return await conn.ExecuteAsync(queryText);
    }

    public async Task<T?> QueryFirstAsync<T>(string queryText) where T : class
    {
        using var conn = await Conn();

        return await conn.QueryFirstOrDefaultAsync<T>(queryText);
    }

    public async Task<T?> QueryValueAsync<T>(string queryText)
    {
        using var conn = await Conn();

        return await conn.ExecuteScalarAsync<T>(queryText);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string queryText)
    {
        using var conn = await Conn();

        return await conn.QueryAsync<T>(queryText);
    }

    public async Task StartupDbConnection()
    {
        // Check DB connection
        if (!File.Exists(Config.SqliteDbFilePath))
        {
            Output.WriteLine($"{Output.Ansi.Cyan} ⬭ No SQLite database was found at: {Config.SqliteDbFilePath}, a new one will be created.");
        }

        try
        {
            Output.WriteLine(" ⬭ Initializing SQLite DB...");

            SQLitePCL.Batteries_V2.Init();

            // Directory must exist first
            var dir = Path.GetDirectoryName(Path.GetFullPath(Config.SqliteDbFilePath));
            if (dir is not null && !Directory.Exists(dir))
            {
                Output.WriteLine($"{Output.Ansi.Cyan} ⬭ Creating directory for SQLite db: {dir}");
                Directory.CreateDirectory(dir);
            }

            var sqliteVersion = (await QueryValueAsync<string?>(@"SELECT sqlite_version();"))?.ToString();

            Output.WriteLine($"{Output.Ansi.Green} ⬭ SQLite engine version {sqliteVersion} connected.");

            // Schema check / setup
            var schSw = Stopwatch.StartNew();
            var (prevVer, newVer) = await ApplySchema();
            schSw.Stop();

            if (prevVer != newVer)
            {
                Output.WriteLine($"{Output.Ansi.Cyan} ⬭ Schema was upgraded from version {prevVer} to {newVer} in {schSw.ElapsedMilliseconds:#,#0} ms.");
            }

            Output.WriteLine($"{Output.Ansi.Green} ⬭ Schema is up to date (at version {newVer}).");
        }
        catch (Exception ex)
        {
            Output.WriteLine($"{Output.Ansi.Red} ⬭ SQLite error: {ex.GetType().FullName}: {ex.Message}");
            Output.WriteLine($"{Output.Ansi.Dim}{Output.Ansi.Red} ⬭   Does this app have read/write permission to the db file path?");
            Environment.Exit(4);
        }
    }

    public async Task<(int prevVer, int newVer)> ApplySchema()
    {
        // Ensure folder exists (do nothing if not)
        if (!Directory.Exists(Globals.DEFAULT_SCHEMA_DIR))
        {
            throw new ApageeException($"DB Schema folder was not found (at {Globals.DEFAULT_SCHEMA_DIR}). Is code deployment incomplete?");
        }

        // Get all *.sql files, sort numerically by filename
        var migrations = Directory.GetFiles(Globals.DEFAULT_SCHEMA_DIR, "*.sql")
            .Select(path => new
            {
                Path = path,
                Version = int.TryParse(Path.GetFileNameWithoutExtension(path), out var v) ? v : (int?)null
            })
            .Where(m => m.Version is not null)
            .Select(m => new { m.Path, Version = (int)m.Version! })
            .OrderBy(x => x.Version)
            .ToList();

        if (Globals.IsVerboseDevelopment)
        {
            Output.WriteLine(" ⬭ Schema file list:\n" + string.Join("\n", migrations.Select(m => $"[{m.Version}] {m.Path}")));
        }

        var prevVersion = await QueryValueAsync<int?>("PRAGMA user_version;") ?? 0;

        if (prevVersion is 0)
        {
            Output.WriteLine($"{Output.Ansi.Yellow} ⬭  ~ New DB detected, applying schema checkpoints...");
        }
        else if (Env.IsDevelopment())
        {
            Output.WriteLine($"{Output.Ansi.Yellow} ⬭  ~ DB current schema version: {prevVersion}");
        }

        var newVersion = 0;
        using var conn = await Conn();
        foreach (var migr in migrations)
        {
            var version = migr.Version;
            newVersion = version;

            if (version <= prevVersion)
            {
                if (Env.IsDevelopment())
                {
                    Output.WriteLine($"{Output.Ansi.Yellow} ⬭  ~ Skipping schema version {migr.Version} (file {migr.Path})");
                }

                continue;
            }

            var sql = await File.ReadAllTextAsync(migr.Path);
            await using var tx = await conn.BeginTransactionAsync();

            await conn.ExecuteAsync(sql, transaction: tx);
            await conn.ExecuteAsync($"PRAGMA user_version = {version};", transaction: tx);

            await tx.CommitAsync();

            Output.WriteLine($"{Output.Ansi.Cyan} ⬭  ~ Applied DB schema: {migr.Version}");
        }

        return (prevVersion, newVersion);
    }
}