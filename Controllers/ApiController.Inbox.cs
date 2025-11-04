namespace Apagee.Controllers;

public partial class ApiController : BaseController
{
    [HttpGet]
    [Route("api/users/{username}/outbox")]
    public async Task<IActionResult> GetOutbox([FromRoute] string username, [FromQuery] bool page = false, [FromQuery] string? max = null, [FromQuery] string? min = null)
    {
        // Single-user only
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound404(message: "User not found.");
        }

        var basePathPaged = $"{CurrentPath}?page=true";

        var oc = new APubOrderedCollection
        {
            Id = CurrentAtomId,
            TotalItems = await ArticleService.GetCount(true)
        };

        var ocp = new APubOrderedCollectionPage
        {
            Id = CurrentAtomId,
            PartOf = new APubLink(CurrentActor.Outbox),
            Items = new()
        };


        if (!page)
        {
            oc.First = basePathPaged;
            oc.Last = $"{basePathPaged}&min={Ulid.MinValue}";

            return Ok(oc);
        }
        else
        {
            IEnumerable<Article> articles;
            if (min is null && max is null)
            {
                articles = await ArticleService.GetOlderThan();
                if (articles.Any())
                {
                    ocp.Next = new APubLink($"{basePathPaged}&max={articles.Last().Uid}");
                    ocp.Prev = new APubLink($"{basePathPaged}&max={articles.First().Uid}");
                }
            }
            else if (min is null)
            {
                articles = await ArticleService.GetOlderThan(max);
                if (articles.Any())
                {
                    ocp.Next = new APubLink($"{basePathPaged}&max={articles.Last().Uid}");
                    ocp.Prev = new APubLink($"{basePathPaged}&max={articles.First().Uid}");
                }
            }
            else if (max is null)
            {
                articles = await ArticleService.GetNewerThan(min);
                if (articles.Any())
                {
                    ocp.Next = new APubLink($"{basePathPaged}&min={articles.Last().Uid}");
                    ocp.Prev = new APubLink($"{basePathPaged}&min={articles.First().Uid}");
                }
            }
            else
            {
                return BadRequest400(message: "Cannot specify both min and max.");
            }

            ocp.Items.AddRange(articles.SelectMany(a => new[] {
                APubActivity.Wrap<Create>(APubStatus.FromArticle(a), CurrentActor.Id, published: a.PublishedOn),
                APubActivity.Wrap<Create>(APubArticle.FromArticle(a), CurrentActor.Id, published: a.PublishedOn)
            }));

            return Ok(ocp);
        }
    }

    [HttpGet]
    [Route("api/users/{username}/inbox")]
    public async Task<IActionResult> GetInbox([FromRoute] string username)
    {
        return NotFound404();
    }

    [HttpPost]
    [Route("api/inbox")]
    public async Task<IActionResult> PostToSharedInbox()
    {
        return await PostToInbox(GlobalConfiguration.Current!.FediverseUsername);
    }

    [HttpPost]
    [Route("api/users/{username}/inbox")]
    public async Task<IActionResult> PostToInbox([FromRoute] string username)
    {
        // Single-user only
        if (username != GlobalConfiguration.Current?.FediverseUsername)
        {
            return NotFound404(message: "User not found.");
        }

        using var sr = new StreamReader(Request.Body);
        var body = await sr.ReadToEndAsync();

        var item = new Inbox
        {
            ID = "",
            UID = Ulid.NewUlid().ToString(),
            BodySize = body.Length,
            BodyData = body,
            Type = "",
            ReceivedOn = DateTime.UtcNow,
            ContentType = Request.ContentType?.ToString() ?? "no/type",
            RemoteServer = Request.Headers["X-Forwarded-For"].ToString() ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };

        JsonNode? jNode = default;
        try
        {
            jNode = await JsonNode.ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes(body)));
            if (jNode?.GetValueKind() == JsonValueKind.Object)
            {
                item.ID = jNode["id"]?.GetValue<string>() ?? "unknown-" + item.UID;
                item.Type = jNode["type"]?.GetValue<string>() ?? "unknown-" + item.UID;
            }
            else
            {
                item.ID = "not-an-obj-" + item.UID;
                item.Type = "not-an-obj-" + item.UID;
            }
        }
        catch
        {
            item.ID = "err-" + item.UID;
            item.Type = "err-" + item.UID;
        }

        if (jNode is JsonObject json)
        {
            if (Request.HasJsonContentType()
                || Request.Headers.ContentType.ToString().ToLower().Contains(Globals.JSON_LD_CONTENT_TYPE)
                || Request.Headers.ContentType.ToString().ToLower().Contains(Globals.JSON_ACT_CONTENT_TYPE))
            {
                
                switch (item.Type)
                {
                    // Receive: Follow
                    case APubConstants.TYPE_ACT_FOLLOW when json["object"] is JsonValue
                        || json["object"] is JsonArray arr && arr[0] is JsonValue:

                        var v = json["object"] as JsonValue ?? (json["object"] as JsonArray)?[0] as JsonValue;
                        if (v is null) break;

                        if (v.GetValue<string>().ToUpper() == ActorId.ToUpper())
                        {
                            var follower = APubFollower.FromJson(json);
                            await InboxService.CreateFollower(follower);
                            Console.WriteLine($"[⁂] « <{item.ID}> {item.Type} from {follower.FollowerId}");

                            // Send: Accept{Follow}
                            await Client.PostInboxFromActor(follower.FollowerId!, new Accept
                            {
                                Id = $"{RootUrl}/{Ulid.NewUlid()}",
                                Actor = ActorId,
                                Object =
                                [
                                    new Follow
                                    {
                                        Id = follower.Id,
                                        Actor = follower.FollowerId,
                                        Object = ActorId
                                    }
                                ]
                            });
                            Console.WriteLine($"[⁂] » <{item.ID}> {APubConstants.TYPE_ACT_ACCEPT} of {APubConstants.TYPE_ACT_FOLLOW} to {follower.FollowerId} via {await Client.GetActorInboxAsync(follower.FollowerId!)}");

                            if (SettingsService.Current?.AutoReciprocateFollows ?? false)
                            {
                                // Store the follow activity ID for later undo
                                var newId = NewActivityId;
                                await KvService.Set(follower.FollowerId ?? follower.FollowerName!, NewActivityId);

                                // Send: Follow (back)
                                await Client.PostInboxFromActor(follower.FollowerId!,
                                    new Follow
                                    {
                                        Id = newId,
                                        Actor = ActorId,
                                        Object = follower.FollowerId
                                    });
                                Console.WriteLine($"[⁂] » <{item.ID}> {APubConstants.TYPE_ACT_FOLLOW} to {follower.FollowerId} via {await Client.GetActorInboxAsync(follower.FollowerId!)}");
                            }
                        }
                        break;

                    // Receive: Undo{Follow}
                    case APubConstants.TYPE_ACT_UNDO when json["object"] is JsonObject obj
                        && obj["id"] is JsonValue origId
                        && obj["object"] is JsonValue origTarget
                        && origId.GetValue<string>() is { Length: > 0 }:
                        if (origTarget.GetValue<string>().ToUpper() == ActorId.ToUpper())
                        {
                            var follower = APubFollower.FromJson(json);
                            await InboxService.DeleteFollower(origId.GetValue<string>());
                            Console.WriteLine($"[⁂] « <{item.ID}> {item.Type} from {follower.FollowerId}");

                            // Send: Accept{Undo}
                            await Client.PostInboxFromActor(follower.FollowerId!, new Accept
                            {
                                Id = NewActivityId,
                                Actor = ActorId,
                                Object =
                                [
                                    new Undo
                                    {
                                        Id = follower.Id,
                                        Actor = follower.FollowerId,
                                        Object = ActorId
                                    }
                                ]
                            });
                            Console.WriteLine($"[⁂] » <{item.ID}> {APubConstants.TYPE_ACT_ACCEPT} of {APubConstants.TYPE_ACT_UNDO} to {follower.FollowerId} via {await Client.GetActorInboxAsync(follower.FollowerId!)}");

                            // Send: Undo{Follow} (back)
                            if (SettingsService.Current?.AutoReciprocateFollows ?? false)
                            {
                                var lastFollowActivityId = await KvService.Get(follower.FollowerId ?? follower.FollowerName!);
                                if (lastFollowActivityId is not null)
                                {
                                    await Client.PostInboxFromActor(follower.FollowerId!,
                                        new Undo
                                        {
                                            Id = NewActivityId,
                                            Actor = ActorId,
                                            Object = new Follow
                                            {
                                                Id = lastFollowActivityId,
                                                Actor = ActorId,
                                                Object = follower.FollowerId
                                            }
                                        });
                                    Console.WriteLine($"[⁂] » <{item.ID}> {APubConstants.TYPE_ACT_UNDO} of {APubConstants.TYPE_ACT_FOLLOW} to {follower.FollowerId} via {await Client.GetActorInboxAsync(follower.FollowerId!)}");
                                }
                            }
                        }
                        break;

                    case APubConstants.TYPE_ACT_ANNOUNCE when json["object"] is JsonValue
                        || json["object"] is JsonArray arr && arr[0] is JsonValue:
                    case APubConstants.TYPE_ACT_LIKE when json["object"] is JsonValue
                        || json["object"] is JsonArray arr2 && arr2[0] is JsonValue:

                        var ann = (json["object"] as JsonValue ?? (json["object"] as JsonArray)?[0] as JsonValue)?.GetValue<string>();
                        if (ann is null) break;

                        var postPath = new PathString(new Uri(ann).AbsolutePath);
                        var postId = postPath.StartsWithSegments($"/api/users/{GlobalConfiguration.Current!.FediverseUsername}", out var nextPath)
                            ? nextPath.Value?.Trim('/')
                            : null;

                        if (postId is null || !Ulid.TryParse(postId, out _))
                        {
                            Console.WriteLine($"[⁂] × <{item.ID}> {APubConstants.TYPE_ACT_ANNOUNCE} with ID {postId} and object URI {ann} is not in the expected format.");
                            break;
                        }

                        var article = await ArticleService.GetByUid(postId);
                        if (article is null)
                        {
                            Console.WriteLine($"[⁂] × <{item.ID}> {APubConstants.TYPE_ACT_ANNOUNCE} with referenced article {postId} was not found in the database.");
                            break;
                        }

                        var interaction = Interaction.FromJson(json, article);

                        if (interaction is null)
                        {
                            Console.WriteLine($"[⁂] × <{item.ID}> {APubConstants.TYPE_ACT_ANNOUNCE} could not be converted to an interaction object.");
                            break;
                        }

                        if (!await InteractionService.InteractionExists(interaction))
                        {
                            await InteractionService.CreateInteraction(interaction);
                        }
                        else
                        {
                            Console.WriteLine($"[⁂] × <{item.ID}> {APubConstants.TYPE_ACT_ANNOUNCE} Interaction already exists.");
                            break;
                        }

                        break;


                    default:
                        Console.WriteLine($"[⁂] « <{item.ID}> {item.Type} (Not handled)\r\n[⁂] ----\r\n{JsonSerializer.Serialize(item)}\r\n[⁂] ----\r\n");
                        break;
                }
            }
        }

        await InboxService.Create(item);

        return Accepted();
    }
}