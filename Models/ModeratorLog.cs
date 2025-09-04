using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarewellMyBeloved.Models;

public class ModeratorLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string ModeratorName { get; set; } = string.Empty;// who performed the action

    // Target can be a user, content, or report. Store both type and id for flexibility.
    [Required]
    [StringLength(50)]
    public string TargetType { get; set; } = string.Empty; // e.g., "user", "content", "report"

    [Required]
    public int TargetId { get; set; }                    // id of the target entity

    [Required]
    public string Action { get; set; } = string.Empty;

    [StringLength(200)]
    public string Reason { get; set; } = string.Empty;   // short reason code or summary

    [Required]
    public string Details { get; set; } = string.Empty;   // free-text details, optional

    // Optional link to a ContentReport if action was related
    public int? ContentReportId { get; set; }

    [ForeignKey(nameof(ContentReportId))]
    public ContentReport? ContentReport { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}