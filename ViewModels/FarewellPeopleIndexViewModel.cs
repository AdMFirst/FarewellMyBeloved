using FarewellMyBeloved.Models;

namespace FarewellMyBeloved.ViewModels;

public class FarewellPeopleIndexViewModel
{
    public List<FarewellPerson> FarewellPeople { get; set; } = new();
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 10;
}