namespace FarewellMyBeloved.ViewModels;

public class AdminIndexViewModel
{
    public ChartDataViewModel FarewellPeopleChartData { get; set; } = new();
    public ChartDataViewModel FarewellMessagesChartData { get; set; } = new();
    public ChartDataViewModel ContentReportsChartData { get; set; } = new();
    public List<ModeratorLogViewModel> ModeratorLogs { get; set; } = new();
    public List<ContentReportViewModel> ContentReports { get; set; } = new();
}

public class ChartDataViewModel
{
    public List<string> Last7DaysLabels { get; set; } = new();
    public List<int> Last7DaysData { get; set; } = new();
    public List<string> Last4WeeksLabels { get; set; } = new();
    public List<int> Last4WeeksData { get; set; } = new();
}

public class ModeratorLogViewModel
{
    public int Id { get; set; }
    public string ModeratorName { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string FormattedCreatedAt => CreatedAt.ToString("dd/MM/yyyy HH:mm");
}

public class ContentReportViewModel
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public int? FarewellPersonId { get; set; }
    public int? FarewellMessageId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string FormattedCreatedAt => CreatedAt.ToString("dd/MM/yyyy HH:mm");
    public string FormattedResolvedAt => ResolvedAt?.ToString("dd/MM/yyyy HH:mm") ?? "Not resolved";
    public string Status => ResolvedAt.HasValue ? "Resolved" : "Pending";
}