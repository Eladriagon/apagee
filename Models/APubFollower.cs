namespace Apagee.Models;

[Table("APubFollowers")]
public class APubFollower
{
    [ExplicitKey]
    public required string Uid { get; set; }
    
    [ExplicitKey]
    public required string Id { get; set; }
    public string? FollowerId { get; set; }
    public string? FollowerName { get; set; }
    public required DateTime CreatedOn { get; set; }

    public static APubFollower FromJson(JsonNode json)
    {
        var ex = new ApageeException("Activity is malformed.");
        var followId = "";
        var followName = "";
        var activityId = "";

        if (json["actor"] is JsonValue v && v.GetValue<string>() is string { Length: > 0 } fid)
        {
            followId = fid;
        }
        else if (json["actor"] is JsonObject obj)
        {
            followName = obj["name"]?.GetValue<string>() ?? throw ex;
        }

        if (json["id"] is JsonValue aid && aid.GetValue<string>() is string { Length: > 0 } actId)
        {
            activityId = actId;
        }
        
        if ((followId is { Length: 0 } 
            && followName is { Length: 0 })
            || activityId is { Length: 0 })
        {
             throw ex;
        }

        return new APubFollower
        {
            Uid = Ulid.NewUlid().ToString(),
            Id = activityId,
            CreatedOn = DateTime.UtcNow,
            FollowerId = followId,
            FollowerName = followName
        };
    }
}