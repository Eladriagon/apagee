using Microsoft.AspNetCore.Identity;

namespace Apagee;

public static class Init
{
    public static async Task<WebApplication> InitApagee(this WebApplication app)
    {
        await app.InitDatabase();
        await app.CheckFirstTimeUserSetup();
        await app.InitKeypair();
        return app;
    }

    public static async Task<WebApplication> InitKeypair(this WebApplication app)
    {
        var keypairHelper = app.Services.GetRequiredService<KeypairHelper>();

        await keypairHelper.TryCreateActorKeypair();

        if (keypairHelper.ActorRsaPrivateKey is null || keypairHelper.ActorRsaPublicKey is null)
        {
            if (Globals.CanRunWithoutKeys)
            {
                Output.WriteLine($"{Output.Ansi.Yellow} % Warning: HttpSig keypair is missing - federation will not work.");
                Output.WriteLine($"{Output.Ansi.Yellow} % Continuing startup because {Globals.ENV_UNSAFE_KEYS} is set.");
            }
            else
            {
                Output.WriteLine($"{Output.Ansi.Red} % HttpSig keypair is missing - federation will definitely not work.");
                Output.WriteLine($"{Output.Ansi.Red} % The app will now exit to protect itself from federation errors.");
                Output.WriteLine($"{Output.Ansi.Red} % You can override this behavior by setting this environment variable: (not recommended)");
                Output.WriteLine($"{Output.Ansi.Red} %   {Globals.ENV_UNSAFE_KEYS}=1");
                Environment.Exit(6);
            }
        }
        else
        {
            Output.WriteLine($"{Output.Ansi.Green} % HttpSig keys loaded.");
        }

        return app;
    }
    
    public static async Task<WebApplication> InitDatabase(this WebApplication app)
    {
        try
        {
            await app.Services.GetRequiredService<StorageService>().StartupDbConnection();
        }
        catch (ApageeException aex)
        {
            Output.WriteLine($"{Output.Ansi.Red}Startup error: {aex.Message}");
            if (aex.InnerException is not null)
            {
                Output.WriteLine($"  {Output.Ansi.Red}Nested error: {aex.InnerException.GetType().FullName}: {aex.InnerException.Message}");
            }
            if (app.Environment.IsDevelopment())
            {
                Output.WriteLine(Output.Ansi.Yellow + aex.ToString());
            }
            Environment.Exit(4);
        }
        catch (Exception ex)
        {
            Output.WriteLine($"{Output.Ansi.Red}Unknown startup error: {ex.GetType().FullName}: {ex.Message}");
            if (ex.InnerException is not null)
            {
                Output.WriteLine($"  {Output.Ansi.Red}Nested error: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
            }
            if (app.Environment.IsDevelopment())
            {
                Output.WriteLine(Output.Ansi.Yellow + ex.ToString());
            }
            Environment.Exit(5);
        }

        return app;

    }

    public static async Task<WebApplication> CheckFirstTimeUserSetup(this WebApplication app)
    {
        var userService = app.Services.GetRequiredService<UserService>();

        var user = await userService.GetUser();

        if (user is null)
        {
            var newPass = RandomUtils.GetRandomAlphanumeric(24);

            var newUser = new User
            {
                Uid = Guid.NewGuid().ToString(),
                PassHash = "",
                Username = "admin"
            };

            await userService.UpsertUser(newUser, newPass);

            Output.WriteLine($"""
                {Output.Ansi.Magenta}
                  +-------------------------------------------------+
                  | No user was detected, auto-generating...        |
                  | (This information will only be displayed once.) |
                  |                                                 |
                  |   User: {Output.Ansi.Bold}admin{Output.Ansi.Reset}{Output.Ansi.Magenta}                                   |
                  |   Pass: {Output.Ansi.Bold}{newPass}{Output.Ansi.Reset}{Output.Ansi.Magenta}                |
                  |                                                 |
                  +-------------------------------------------------+
                
                """);
        }

        return app;
    }
}