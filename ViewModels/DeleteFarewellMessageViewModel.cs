using FarewellMyBeloved.Models;
using System.ComponentModel.DataAnnotations;

namespace FarewellMyBeloved.ViewModels;

// This is view model for the post request
public class DeleteFarewellMessageViewModel
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
    public int FarewellPersonId { get; set; }
    public string FarewellPersonName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsPublic { get; set; } = true;

    // For content report linking
    public List<ContentReport>? RelatedContentReports { get; set; } = new();

    public Guid? SelectedContentReportId { get; set; }

    // For logging
    [Required(ErrorMessage = "Action reason is required")]
    public string? ActionReason { get; set; }

    [Required(ErrorMessage = "Action details are required")]
    public string? ActionDetails { get; set; }
}