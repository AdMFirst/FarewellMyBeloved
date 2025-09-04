using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarewellMyBeloved.Models;

public class ContentReport
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    [Required(ErrorMessage = "Email is required")]
    public string? Email { get; set; }

    public int? FarewellPersonId { get; set; }

    public int? FarewellMessageId { get; set; }

    [Required(ErrorMessage = "Reason is required")]
    [StringLength(100, ErrorMessage = "Reason cannot exceed 100 characters")]
    public string Reason { get; set; } = string.Empty; // e.g., "spam", "abuse", "other"

    public string? Explanation { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    // Link to moderator actions that handled or referenced this report
    public ICollection<ModeratorLog>? ModeratorLogs { get; set; }
}