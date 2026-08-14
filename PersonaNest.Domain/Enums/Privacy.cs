namespace PersonaNest.Domain.Enums;

/// <summary>Visibility of an Entry or a Collection. Specification v3 §3.</summary>
public enum Privacy
{
    Public = 0,
    FollowersOnly = 1,
    Private = 2
}
