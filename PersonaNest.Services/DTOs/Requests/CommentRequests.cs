using System.ComponentModel.DataAnnotations;

namespace PersonaNest.Services.DTOs.Requests;

/// <summary>Posting a comment or a one-level-deep reply on an Entry (§5, §18).</summary>
public sealed class CreateCommentRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int EntryId { get; set; }

    /// <summary>Null for a top-level comment. Set only when replying to a top-level comment.</summary>
    public int? ParentCommentId { get; set; }

    [Required(ErrorMessage = "Write something before posting.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Comments can be up to 2000 characters.")]
    public string Content { get; set; } = string.Empty;
}
