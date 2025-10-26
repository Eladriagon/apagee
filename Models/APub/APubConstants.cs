namespace Apagee.Models.APub;

public static class APubConstants
{
    // Base
    public const string TYPE_OBJECT = "Object";
    public const string TYPE_LINK = "Link";

    // Collections
    public const string TYPE_COLLECTION = "Collection";
    public const string TYPE_COLLECTION_ORDERED = "OrderedCollection";
    public const string TYPE_COLLECTION_PAGE = "CollectionPage";
    public const string TYPE_COLLECTION_PAGE_ORDERED = "OrderedCollectionPage";

    // Objects
    public const string TYPE_OBJ_ACTOR = "Actor";
    public const string TYPE_OBJ_ACTIVITY = "Activity";
    public const string TYPE_OBJ_ACTIVITY_INT = "IntransitiveActivity";
    public const string TYPE_OBJ_IMAGE = "Image";
    public const string TYPE_OBJ_NOTE = "Note";
    public const string TYPE_OBJ_ARTICLE = "Article";

    // Actors
    public const string TYPE_ID_PERSON = "Person";
    public const string TYPE_ID_GROUP = "Group";
    public const string TYPE_ID_APP = "Application";
    public const string TYPE_ID_SERVICE = "Service";
    public const string TYPE_ID_ORG = "Organization";

    // Activities
    public const string TYPE_ACT_ACCEPT = "Accept";
    public const string TYPE_ACT_ADD = "Add";
    public const string TYPE_ACT_ANNOUNCE = "Announce";
    public const string TYPE_ACT_ARRIVE = "Arrive";
    public const string TYPE_ACT_BLOCK = "Block";
    public const string TYPE_ACT_CREATE = "Create";
    public const string TYPE_ACT_DELETE = "Delete";
    public const string TYPE_ACT_DISLIKE = "Dislike";
    public const string TYPE_ACT_FLAG = "Flag";
    public const string TYPE_ACT_FOLLOW = "Follow";
    public const string TYPE_ACT_IGNORE = "Ignore";
    public const string TYPE_ACT_INVITE = "Invite";
    public const string TYPE_ACT_JOIN = "Join";
    public const string TYPE_ACT_LEAVE = "Leave";
    public const string TYPE_ACT_LIKE = "Like";
    public const string TYPE_ACT_LISTEN = "Listen";
    public const string TYPE_ACT_MOVE = "Move";
    public const string TYPE_ACT_OFFER = "Offer";
    public const string TYPE_ACT_QUESTION = "Question";
    public const string TYPE_ACT_REJECT = "Reject";
    public const string TYPE_ACT_READ = "Read";
    public const string TYPE_ACT_REMOVE = "Remove";
    public const string TYPE_ACT_TENTATIVEREJECT = "TentativeReject";
    public const string TYPE_ACT_TENTATIVEACCEPT = "TentativeAccept";
    public const string TYPE_ACT_TRAVEL = "Travel";
    public const string TYPE_ACT_UNDO = "Undo";
    public const string TYPE_ACT_UPDATE = "Update";
    public const string TYPE_ACT_VIEW = "View";

    public const string CTX_ACT_STREAM = "https://www.w3.org/ns/activitystreams";
    public const string TARGET_PUBLIC = "https://www.w3.org/ns/activitystreams#Public";
}