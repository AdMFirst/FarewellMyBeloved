using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FarewellMyBeloved.ViewModels;

public class CreateFarewellPersonViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string Description { get; set; } = string.Empty;

    public bool UsePortraitUrl { get; set; }

    [StringLength(500, ErrorMessage = "Portrait URL cannot exceed 500 characters")]
    public string? PortraitUrl { get; set; }

    public IFormFile? PortraitFile { get; set; }

    public bool UseBackgroundUrl { get; set; }

    [StringLength(500, ErrorMessage = "Background URL cannot exceed 500 characters")]
    public string? BackgroundUrl { get; set; }

    public IFormFile? BackgroundFile { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string? Email { get; set; }
}