namespace PersonaNest.Domain.Enums;

/// <summary>Why a user reported a piece of content. Specification v3 §3.</summary>
public enum ReportReason
{
    Spam = 0,
    Inappropriate = 1,
    Duplicate = 2,
    IncorrectInfo = 3,
    Harassment = 4,
    Other = 5
}
