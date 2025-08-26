using System.ComponentModel.DataAnnotations;

namespace FarewellMyBeloved.ViewModels;

public class CreateFarewellPersonViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string Description { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Portrait URL cannot exceed 500 characters")]
    public string? PortraitUrl { get; set; }

    [StringLength(500, ErrorMessage = "Background URL cannot exceed 500 characters")]
    public string? BackgroundUrl { get; set; }
}