using FarewellMyBeloved.Models;

namespace FarewellMyBeloved.ViewModels;

public class FarewellMessagesIndexViewModel
{
    public List<FarewellMessage> FarewellMessages { get; set; } = new();
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 10;
}