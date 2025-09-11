using FarewellMyBeloved.Models;
using System.ComponentModel.DataAnnotations;

namespace FarewellMyBeloved.ViewModels;

// This is view model for the post request
public class DeleteFarewellPersonViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PortraitUrl { get; set; } = string.Empty;
    public string BackgroundUrl { get; set; } = string.Empty;
    public List<FarewellMessage>? FarewellMessages { get; set; } = new();
    public List<ContentReport>? RelatedContentReports { get; set; } = new();

    public Guid? SelectedContentReportId { get; set; }

    // For logging
    [Required(ErrorMessage = "Action reason is required")]
    public string? ActionReason { get; set; }

    [Required(ErrorMessage = "Action details are required")]
    public string? ActionDetails { get; set; }
}