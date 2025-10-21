using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Apagee.Configuration;

/// <summary>
/// Manages retrieving, parsing, and validating the global configuration object.
/// </summary>
public static partial class ConfigManager
{
    /// <summary>
    /// Gets the override file path from the environment variable.
    /// </summary>
    public static string? OverridePath => Environment.GetEnvironmentVariable(Globals.ENV_CONFIG_PATH);

    /// <summary>
    /// Gets the relative path to the configuration file.
    /// </summary>
    public static string ConfigPath => OverridePath is not null && File.Exists(OverridePath) ? OverridePath : Globals.DEFAULT_CONFIG_PATH;

    /// <summary>
    /// Stores the current global configuration.
    /// </summary>
    private static GlobalConfiguration? ConfigFile { get; set; }

    private static JsonDocumentOptions ReadOptions => new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        MaxDepth = 8
    };

    private static JsonSerializerOptions SerializerOptions => new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
        RespectNullableAnnotations = true
    };

    public static async Task Init()
    {
        try
        {
            ConfigFile = new();

            if (File.Exists(ConfigPath))
            {
                var jsonIn = await JsonNode.ParseAsync(File.OpenRead(ConfigPath), documentOptions: ReadOptions);
                if (jsonIn is not JsonObject cfg)
                {
                    throw new ApageeException($"Config file is not a JSON object (found {jsonIn?.GetType().Name ?? "NULL"} instead).");
                }

                ConfigFile = cfg.Deserialize<GlobalConfiguration>(SerializerOptions);
            }

            // Load any env var overrides
            foreach (var prop in typeof(GlobalConfiguration).GetProperties())
            {
                var envOverride = Environment.GetEnvironmentVariable(ConvertPropertyToEnvVar(prop));

                if (envOverride is not null)
                {
                    prop.SetValue(ConfigFile, Convert.ChangeType(envOverride, prop.PropertyType));
                }
            }
        }
        catch (IOException ioEx)
        {
            throw new ApageeException($"Cannot read config file at {ConfigPath} - check permissions.", ioEx);
        }
    }

    public static List<(string propertyName, string errorMessage, bool warningOnly)> ValidateGlobalConfig(GlobalConfiguration config)
    {
        var issues = new List<(string propertyName, string errorMessage, bool warningOnly)>();

        // Validate: HTTP Binding
        if (config.HttpBindUrl is not { Length: > 0 })
        {
            issues.Add((nameof(config.HttpBindUrl), "Value was not set, will default to http://127.0.0.1:80", true));
        }

        try
        {
            var uri = new Uri(config.HttpBindUrl, UriKind.Absolute);
            if (uri.Scheme.ToUpper() is not "HTTP" or "HTTPS")
            {
                throw new Exception();
            }
        }
        catch
        {
            issues.Add((nameof(config.HttpBindUrl), "Value must be a URL (e.g. http://127.0.0.1:80 or https://*:443)", true));
        }

        // Validate: SQLite DB path
        if (config.SqliteDbFilePath is not { Length: > 0 })
        {
            issues.Add((nameof(config.SqliteDbFilePath), "SQLite db file path is not set.", false));
        }

        // Validate: Fediverse Username
        if (config.FediverseUsername is not { Length: > 0 })
        {
            issues.Add((nameof(config.FediverseUsername), "Value not set.", false));
        }
        else if ((config.FediverseUsername ?? "").StartsWith('@'))
        {
            issues.Add((nameof(config.FediverseUsername), "Value should not start with an '@' character (it is automatically added).", false));
        }
        else if (!Globals.RgxFediverseUsername().IsMatch(config.FediverseUsername ?? string.Empty))
        {
            issues.Add((nameof(config.FediverseUsername), "Value can only contain letters, numbers, and underscores with a max length of 30.", false));
        }

        // Validate: Public Hostname
        if (config.PublicHostname is not { Length: > 0 })
        {
            issues.Add((nameof(config.PublicHostname), "Value is not set.", false));
        }
        else
        {
            try
            {
                if (!config.PublicHostname.Contains('.'))
                {
                    throw new Exception();
                }

                _ = new Uri($"https://{config.PublicHostname}", UriKind.Absolute);
            }
            catch
            {
                issues.Add((nameof(config.PublicHostname), "Value is not a valid hostname.", false));
            }
        }

        // Validate: Author Display Name
        if (config.AuthorDisplayName is not { Length: > 0 })
        {
            issues.Add((nameof(config.AuthorDisplayName), "Value is not set.", false));
        }

        // Validate: Fediverse Bio
        if (config.FediverseBio is not { Length: > 0 })
        {
            issues.Add((nameof(config.FediverseBio), "Value is not set.", true));
        }   

        return issues;
    }

    public static GlobalConfiguration GetGlobalConfig()
    {
        return ConfigFile ?? throw new ApageeException("Configuration object is null (did you call Init() successfully?).");
    }

    private static string ConvertPropertyToEnvVar(PropertyInfo prop)
    {
        var name = prop.Name;

        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c) && i > 0 && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
            {
                sb.Append('_');
            }

            sb.Append(char.ToUpperInvariant(c));
        }

        return sb.ToString();
    }

}