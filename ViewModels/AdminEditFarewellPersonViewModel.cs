using FarewellMyBeloved.Models;

namespace FarewellMyBeloved.ViewModels;

// This is view model for the get request
public class AdminEditFarewellPersonViewModel
{
    public EditFarewellPersonViewModel FarewellPerson { get; set; } = new();
    public List<ContentReport> AllContentReports { get; set; } = new();
    public List<FarewellMessage> RelatedMessages { get; set; } = new();


    // For image previews
    public string? PortraitImageUrl { get; set; }
    public string? BackgroundImageUrl { get; set; }
}