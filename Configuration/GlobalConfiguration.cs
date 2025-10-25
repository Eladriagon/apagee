namespace Apagee.Configuration;

/// <summary>
/// The global configuration object which stores the values read from configuration files
/// and/or environment variables.
/// </summary>
public sealed class GlobalConfiguration
{
    public static GlobalConfiguration? Current { get; set; }

    #region HTTP

    /// <summary>
    /// The URL (host or IP and port) to bind to.
    /// </summary>
    public string HttpBindUrl { get; set; } = "http://127.0.0.1:80";

    /// <summary>
    /// The public DNS name where this site is accessible from.
    /// </summary>
    public string PublicHostname { get; set; } = default!;

    /// <summary>
    /// Specifies if the site does not explicitly use HTTPS or serve a certificate, but is behind a reverse proxy / endpoint manager that terminates TLS.
    /// </summary>
    public bool UsesReverseProxy { get; set; } = false;

    #endregion


    #region Database

    /// <summary>
    /// The relative path to the SQLite database file to use.
    /// </summary>
    public string SqliteDbFilePath { get; set; } = "db/apagee.db";

    #endregion


    #region Theming

    /// <summary>
    /// If set to <see langword="true" />, the site will always appear in dark mode.
    /// Otherwise, uses the browser's theme preference.
    /// </summary>
    public bool ForceDarkMode { get; set; } = false;

    #endregion


    #region Author and Fediverse

    /// <summary>
    /// The username to use when federating content and answering ActivityPub queries.
    /// </summary>
    public string FediverseUsername { get; set; } = default!;

    /// <summary>
    /// The display name to use when publishing articles, and the friendly fediverse profile name.
    /// Will use the <see cref="FediverseUsername" /> if not specified.
    /// </summary>
    public string? AuthorDisplayName { get; set; }

    /// <summary>
    /// The summary/bio for your fediverse profile. Can optionally be displayed on the blog as well.
    /// </summary>
    public string? FediverseBio { get; set; }

    /// <summary>
    /// Whether or not to include the sumamry/bio text in the author block of the article.
    /// </summary>
    public bool ShowBioOnArticles { get; set; } = false;

    /// <summary>
    /// Whether or not to include the profile picture in the author block of the article.
    /// </summary>
    public bool ShowProfilePictureOnArticles { get; set; } = true;

    /// <summary>
    /// The language code preference for Fediverse activities.
    /// </summary>
    public string? LanguageCode { get; set; } = "en";

    #endregion


    #region Helpers

    /// <summary>
    /// The "connection string" for SQLite.
    /// </summary>
    [JsonIgnore]
    public string SqliteConnectionString => $"Data Source={Path.GetFullPath(SqliteDbFilePath)};";
    
    #endregion
}