namespace PersonaNest.Domain.Constants;

/// <summary>
/// The three Identity role names (§6). Held in Domain so the seeder, the services and the
/// <c>[Authorize(Roles = ...)]</c> attributes all name the same strings.
/// </summary>
public static class Roles
{
    public const string User = "User";
    public const string Moderator = "Moderator";
    public const string Admin = "Admin";

    public static readonly string[] All = { User, Moderator, Admin };
}
