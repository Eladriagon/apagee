using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;

Output.WriteLine($"{Output.Ansi.Blue}Apagee Blog Server{Output.Ansi.Reset} - v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?.?.?"}");

GlobalConfiguration config = default!;
try
{
    Output.WriteLine("Loading configuration...");

    // Load and validate global configuration
    await ConfigManager.Init();
    config = ConfigManager.GetGlobalConfig();
    var configErrors = ConfigManager.ValidateGlobalConfig(config);

    if (configErrors.Count != 0)
    {
        foreach (var (propertyName, errorMessage, warningOnly) in configErrors)
        {
            Output.WriteLine($"{(warningOnly ? Output.Ansi.Yellow : Output.Ansi.Red)} {(warningOnly ? "»" : ">")} [Config {(warningOnly ? "warning" : "error")}] {propertyName}: {errorMessage}");
        }

        if (configErrors.Any(c => !c.warningOnly))
        {
            Environment.Exit(3);
        }
    }

    GlobalConfiguration.Current = config;

    Output.WriteLine($"{Output.Ansi.Green}Configuration loaded successfully.");
}
catch (ApageeException aex)
{
    Output.WriteLine($"{Output.Ansi.Red}Init error: {aex.Message}");
    if (aex.InnerException is not null)
    {
        Output.WriteLine($"  {Output.Ansi.Red}Nested error: {aex.InnerException.GetType().FullName}: {aex.InnerException.Message}");
    }
    Environment.Exit(1);
}
catch (Exception ex)
{
    Output.WriteLine($"{Output.Ansi.Red}Unknown init error: {ex.GetType().FullName}: {ex.Message}");
    if (ex.InnerException is not null)
    {
        Output.WriteLine($"  {Output.Ansi.Red}Nested error: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
    }
    Environment.Exit(2);
}

int eCode = 0;
try
{
    var builder = WebApplication.CreateBuilder(args);

    // This is super noisy.
    builder.Logging.ClearProviders();

    // Bind Kestrel directly to port 80 (no IIS)
    builder.WebHost.ConfigureKestrel(o =>
    {
        o.AddServerHeader = false;
        o.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB, subject to change or via config?
        o.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(60);
    });

    builder.WebHost.UseUrls(config.HttpBindUrl);

    builder.Services.AddMemoryCache();

    builder.Services
        .AddDataProtection()
        .SetApplicationName(Globals.APP_NAME)
        .PersistKeysToFileSystem(new DirectoryInfo(Globals.KeyringDir));

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(cookie =>
                    {
                        cookie.LoginPath = "/admin/login";
                        cookie.AccessDeniedPath = "/403";

                        cookie.Cookie.Name = "apagee_login";
                        cookie.Cookie.HttpOnly = true;
                        cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

                        cookie.Cookie.SameSite = SameSiteMode.Strict;

                        cookie.SlidingExpiration = true;
                        cookie.ExpireTimeSpan = TimeSpan.FromDays(1); // TODO: Allow configuration
                    });

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddAuthorization();

    builder.Services.AddRazorComponents();

    var mvcBuilder = builder.Services.AddControllersWithViews(opt =>
    {
        // Injects @context
        opt.Filters.Add<ContextResponseWrapperFilter>();
    })
    .AddMvcOptions(opts =>
    {
        var jsonFormatter = opts.OutputFormatters.FirstOrDefault(f => f is SystemTextJsonOutputFormatter);

        if (jsonFormatter is SystemTextJsonOutputFormatter jf)
        {
            jf.SupportedMediaTypes.Clear();
            jf.SupportedMediaTypes.Add("application/json");
            jf.SupportedMediaTypes.Add(Globals.JSON_ACT_CONTENT_TYPE);
            jf.SupportedMediaTypes.Add(Globals.JSON_LD_CONTENT_TYPE);
            jf.SupportedMediaTypes.Add(Globals.JSON_LD_CONTENT_TYPE_TRIM);
            jf.SupportedMediaTypes.Add(Globals.JSON_NODEINFO_CONTENT_TYPE);
            jf.SupportedMediaTypes.Add(Globals.JSON_RD_CONTENT_TYPE);
        }
    })
    .AddJsonOptions(json =>
    {
        // Sets up serialization options
        APubJsonOptions.OptionModifier(json.JsonSerializerOptions);
    });

    if (builder.Environment.IsDevelopment())
    {
        mvcBuilder = mvcBuilder.AddRazorRuntimeCompilation();
    }
    
    builder.Services.AddScoped<FediverseSigningHandler>();
    builder.Services.AddHttpClient(Globals.HTTP_CLI_NAME_FED)
                    .AddHttpMessageHandler<FediverseSigningHandler>();
    
    builder.Services.AddSingleton(config);
    builder.Services.AddSingleton(new FluidParser());
    builder.Services.AddSingleton(_ =>
    {
        var opts = new TemplateOptions
        {
            MemberAccessStrategy = new UnsafeMemberAccessStrategy()
        };
        return opts;
    });

    builder.Services.AddSingleton<SettingsService>();
    builder.Services.AddSingleton<StorageService>();
    builder.Services.AddSingleton<KeyValueService>();
    builder.Services.AddSingleton<ArticleService>();
    builder.Services.AddSingleton<UserService>();
    builder.Services.AddSingleton<InboxService>();
    builder.Services.AddSingleton<FedClient>();
    builder.Services.AddSingleton(provider =>
    {
        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        APubJsonOptions.OptionModifier(opts); // your existing modifier
        return opts;
    });
    
    builder.Services.AddSingleton<KeypairHelper>();
    builder.Services.AddSingleton<SecurityHelper>();

    var app = builder.Build();

    // Custom app init code
    await app.InitApagee();

    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            if (ex is ApageeException aex)
            {
                Console.WriteLine($" ⚠ Apagee Error: {ex.Message}\n{ex.StackTrace}");
            }
            else
            {
                Console.WriteLine($"[UNHANDLED EXCEPTION] {ex}");
            }
            Console.ResetColor();

            // Optional: prevent rethrowing so the app doesn’t kill the connection
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Internal Server Error");
        }
    });

    // -----------------------
    // Request pipeline:
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UNHANDLED EXCEPTION] --- {ex}");
            Console.WriteLine($"");

            // Optional: prevent rethrowing so the app doesn’t kill the connection
            try
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Unhandled server error in Apagee (check console).");
            }
            catch
            {
                // Skip - not required.
            }
        }
    });
    if (config.UsesReverseProxy)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
        });
    }

    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    // Attribute routing
    // Covers most system pages
    app.MapControllers();

    // Article "permalink" view URL
    app.MapControllerRoute("page", "{*slug}", new { controller = "Page", action = "Get" });

    //app.UseStatusCodePagesWithReExecute();

    app.MapFallback(() => Results.LocalRedirect("404"));

    Output.WriteLine($"{Output.Ansi.White}Apagee is now running! ({config.HttpBindUrl})");

    app.Run();
}
catch (Exception ex)
{
    eCode = -1;

    var errorAnsiPrefix = $"   {Output.Ansi.BgRed}{Output.Ansi.White} {Output.Ansi.Underline}/!\\{Output.Ansi.Reset}{Output.Ansi.BgRed}{Output.Ansi.White}";

    Output.WriteLine("");
    Output.WriteLine($"{errorAnsiPrefix} [Global unhandled error] {ex.GetType().FullName} ");
    Output.WriteLine($"{errorAnsiPrefix} {ex.Message} ");
    Output.WriteLine("");
    Output.WriteLine($"{Output.Ansi.Red}{ex.StackTrace}");
}
finally
{
    Output.WriteLine("");
    Output.WriteLine("Apagee has exited.");
    Output.WriteLine("");
    Environment.Exit(eCode);
}