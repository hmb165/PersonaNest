namespace PersonaNest.Domain.Enums;

/// <summary>Lifecycle of a ModeratorApplication. Specification v3 §7.</summary>
public enum ApplicationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
