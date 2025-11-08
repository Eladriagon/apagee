namespace Apagee.Controllers;

[ApiController]
[ActivityContentType]
public partial class ApiController(ArticleService articleService,
                                   KeypairHelper keypairHelper,
                                   IMemoryCache cache,
                                   InboxService inboxService,
                                   InteractionService interactionService,
                                   FedClient client,
                                   KeyValueService kvService,
                                   JsonSerializerOptions opts)
    : BaseController
{
    public ArticleService ArticleService { get; } = articleService;
    public KeypairHelper KeypairHelper { get; } = keypairHelper;
    public IMemoryCache Cache { get; } = cache;
    public InboxService InboxService { get; } = inboxService;
    public InteractionService InteractionService { get; } = interactionService;
    public FedClient Client { get; } = client;
    public KeyValueService KvService { get; } = kvService;
    public JsonSerializerOptions Opts { get; } = opts;

    private string NewActivityId => $"{RootUrl}/{Ulid.NewUlid()}";
    private string RootUrl => $"https://{GlobalConfiguration.Current?.PublicHostname}";
    private string ActorId => $"{RootUrl}/api/users/{GlobalConfiguration.Current!.FediverseUsername}";
    private string CurrentPath => $"{RootUrl}{Request.Path}";
    private string CurrentAtomId => CurrentPath + Request.QueryString;
    private APubActor CurrentActor => APubActor.Create<Person>(KeypairHelper.KeyId, KeypairHelper.ActorPublicKeyPem ?? throw new ApageeException("Cannot create user: Actor public key PEM is null."));

    private bool RequestIsForHtml()
        => Request.Headers.Accept.ToString().ToLower().Contains("text/html");
    

    private bool TryRedirectHtml(object obj, out string redirect)
    {
        redirect = "";
        if (RequestIsForHtml())
        {
            var polyUrl = obj is APubObject o
                ? o.Url
                : obj is APubLink l
                    ? l.Url
                    : obj is APubPolyBase { Count: 1 } p
                        ? (p[0].IsLink ? ((APubLink)p[0]).Url : p[0].IsObject ? ((APubObject)p[0]).Url : null)
                        : null;

            var target = polyUrl is { Count: 1 } && polyUrl[0].IsLink && ((APubLink)polyUrl[0]).Href is not null ? ((APubLink)polyUrl[0]).Href : null;
            if (target is not null)
            {
                redirect = target.PathAndQuery;
                return true;
            }
        }
        return false;
    }

}