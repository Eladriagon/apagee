namespace Apagee;

public partial class Globals
{
    internal const string APP_NAME = "Apagee";
    internal const string KEYRING_NAME = "Keys";
    internal const string ENC_KEY_ID = "Apagee.v1.key";
    internal const string HTTP_CLI_NAME_FED = "Fediverse";

    // Warning: Don't change this or federation may break for some legacy clients.
    internal const int RSA_KEY_STRENGTH = 2048;

    // Env Vars
    internal const string ENV_CONFIG_PATH = "APAGEE_CFG_PATH";
    internal const string ENV_KEYRING_DIR = "APAGEE_KEYRING_DIR";
    internal const string ENV_UNSAFE_KEYS = "APAGEE_UNSAFE_RUN_WITHOUT_KEYS";

    internal const string DEFAULT_KEYRING_DIR = ".keys";
    internal const string DEFAULT_CONFIG_PATH = "config.json";
    internal const string DEFAULT_SCHEMA_DIR = "schema/";

    // Computed helpers
    internal static string KeyringDir => Path.GetFullPath(Environment.GetEnvironmentVariable(ENV_KEYRING_DIR) ?? DEFAULT_KEYRING_DIR);

    internal static readonly bool IsVerboseDevelopment = Environment.GetEnvironmentVariable("APAGEE_DEV_VERBOSE") is "true";

    internal static readonly bool CanRunWithoutKeys = Environment.GetEnvironmentVariable(ENV_UNSAFE_KEYS) is "1" or "true";

    // Regex
    [GeneratedRegex(@"^[a-zA-Z0-9_]{1,30}$")]
    public static partial Regex RgxFediverseUsername();
}