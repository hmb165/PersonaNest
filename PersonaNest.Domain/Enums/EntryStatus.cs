using System.ComponentModel.DataAnnotations;

namespace PersonaNest.Domain.Enums;

/// <summary>
/// Where the user is with a piece of media. <see cref="InProgress"/> is deliberately
/// category-neutral: PersonaNest spans games, books, manga and music, so "Watching" would be
/// wrong for most of the catalogue. Specification v3 §3 (decision D-15).
/// </summary>
public enum EntryStatus
{
    Planning = 0,

    [Display(Name = "In Progress")]
    InProgress = 1,

    Completed = 2,
    Paused = 3,
    Dropped = 4
}
