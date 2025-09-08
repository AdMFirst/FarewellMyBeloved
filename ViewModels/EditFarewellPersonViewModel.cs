using FarewellMyBeloved.Models;
using System.ComponentModel.DataAnnotations;

namespace FarewellMyBeloved.ViewModels;

public class EditFarewellPersonViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PortraitUrl { get; set; }
    public string? BackgroundUrl { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsPublic { get; set; } = true;
    
    // For content report linking
    public List<ContentReport>? RelatedContentReports { get; set; } = new();
    public Guid? SelectedContentReportId { get; set; }
    
    // For logging
    [Required(ErrorMessage = "Action reason is required")]
    public string? ActionReason { get; set; }
    
    [Required(ErrorMessage = "Action details are required")]
    public string? ActionDetails { get; set; }
    public string? Action { get; set; } // "edit" or "delete"
}