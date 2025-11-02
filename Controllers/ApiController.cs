namespace Apagee.Controllers;

[ApiController]
[ActivityContentType]
public partial class ApiController(ArticleService articleService, KeypairHelper keypairHelper, IMemoryCache cache, InboxService inboxService, FedClient client, KeyValueService kvService, JsonSerializerOptions opts)
    : BaseController
{
    public ArticleService ArticleService { get; } = articleService;
    public KeypairHelper KeypairHelper { get; } = keypairHelper;
    public IMemoryCache Cache { get; } = cache;
    public InboxService InboxService { get; } = inboxService;
    public FedClient Client { get; } = client;
    public KeyValueService KvService { get; } = kvService;
    public JsonSerializerOptions Opts { get; } = opts;

    private string NewActivityId => $"{RootUrl}/{Ulid.NewUlid()}";
    private string RootUrl => $"https://{GlobalConfiguration.Current?.PublicHostname}";
    private string ActorId => $"{RootUrl}/api/users/{GlobalConfiguration.Current!.FediverseUsername}";
    private string CurrentPath => $"{RootUrl}{Request.Path}";
    private string CurrentAtomId => CurrentPath + Request.QueryString;
    private APubActor CurrentActor => APubActor.Create<Person>(KeypairHelper.KeyId, KeypairHelper.ActorPublicKeyPem ?? throw new ApageeException("Cannot create user: Actor public key PEM is null."));
}