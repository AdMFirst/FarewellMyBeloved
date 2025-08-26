using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarewellMyBeloved.Models;

public class FarewellMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required(ErrorMessage = "FarewellPersonId is required")]
    public int FarewellPersonId { get; set; }

    [StringLength(100, ErrorMessage = "Author name cannot exceed 100 characters")]
    public string? AuthorName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255, ErrorMessage = "Author email cannot exceed 255 characters")]
    public string? AuthorEmail { get; set; }

    [Required(ErrorMessage = "Message is required")]
    [StringLength(2000, ErrorMessage = "Message cannot exceed 2000 characters")]
    public string Message { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public bool IsPublic { get; set; } = true;

    // Navigation property for related person
    [ForeignKey("FarewellPersonId")]
    public virtual FarewellPerson? FarewellPerson { get; set; }
}