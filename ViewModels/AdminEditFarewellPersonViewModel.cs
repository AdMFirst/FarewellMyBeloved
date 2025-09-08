using FarewellMyBeloved.Models;

namespace FarewellMyBeloved.ViewModels;

public class AdminEditFarewellPersonViewModel
{
    public EditFarewellPersonViewModel FarewellPerson { get; set; } = new();
    public List<ContentReport> AllContentReports { get; set; } = new();
    public List<FarewellMessage> RelatedMessages { get; set; } = new();
    
    // For form submission
    public bool IsDeleteConfirmed { get; set; }
    public string? DeleteReason { get; set; }
    
    // For image previews
    public string? PortraitImageUrl { get; set; }
    public string? BackgroundImageUrl { get; set; }
}