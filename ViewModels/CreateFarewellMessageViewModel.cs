using System.ComponentModel.DataAnnotations;

namespace FarewellMyBeloved.ViewModels;

public class CreateFarewellMessageViewModel
{
    [Required(ErrorMessage = "Message is required")]
    [StringLength(2000, ErrorMessage = "Message cannot exceed 2000 characters")]
    public string Message { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string? AuthorName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string? AuthorEmail { get; set; }
}