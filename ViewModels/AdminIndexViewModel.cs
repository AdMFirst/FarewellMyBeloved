namespace FarewellMyBeloved.ViewModels;

public class AdminIndexViewModel
{
    public ChartDataViewModel FarewellPeopleChartData { get; set; } = new();
    public ChartDataViewModel FarewellMessagesChartData { get; set; } = new();
    public ChartDataViewModel ContentReportsChartData { get; set; } = new();
}

public class ChartDataViewModel
{
    public List<string> Last7DaysLabels { get; set; } = new();
    public List<int> Last7DaysData { get; set; } = new();
    public List<string> Last4WeeksLabels { get; set; } = new();
    public List<int> Last4WeeksData { get; set; } = new();
}