using FarewellMyBeloved.Models;

namespace FarewellMyBeloved.ViewModels;

public class ContentReportsIndexViewModel
{
    public List<ContentReport> ContentReports { get; set; } = new();
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 10;
    
    // For related data lookup
    public Dictionary<int, FarewellPerson>? PersonLookup { get; set; }
    public Dictionary<int, FarewellMessage>? MessageLookup { get; set; }
}