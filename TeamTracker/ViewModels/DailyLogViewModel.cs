using Microsoft.AspNetCore.Mvc.Rendering;
using TeamTracker.Models;

namespace TeamTracker.ViewModels;

public class DailyLogIndexViewModel
{
    public List<DailyLog> Logs { get; set; } = new();
    public DateTime? FilterDate { get; set; }
    public int? FilterMemberId { get; set; }
    public string? FilterProject { get; set; }
    public string? FilterStatus { get; set; }
    public SelectList Members { get; set; } = null!;
    public SelectList Projects { get; set; } = null!;
}

public class DailyLogEditViewModel
{
    public DailyLog Log { get; set; } = new();
    public SelectList Members { get; set; } = null!;
    public SelectList Projects { get; set; } = null!;
}
