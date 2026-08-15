using System.ComponentModel.DataAnnotations;
using PersonaNest.Domain.Enums;

namespace PersonaNest.Services.DTOs.Requests;

/// <summary>
/// Reporting a Media item, Entry or Comment (Specification v3 §6). One shape for all three -
/// <see cref="TargetType"/> tells the service which of the three report tables to write to
/// (decision D-4).
/// </summary>
public sealed class CreateReportRequest
{
    [Required]
    public ReportTargetType TargetType { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int TargetId { get; set; }

    [Required(ErrorMessage = "Choose a reason.")]
    public ReportReason Reason { get; set; }
}
