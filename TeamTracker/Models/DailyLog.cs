using System.ComponentModel.DataAnnotations;

namespace TeamTracker.Models;

public class DailyLog
{
    public int Id { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;

    [MaxLength(100)]
    public string Project { get; set; } = string.Empty;

    [MaxLength(100)]
    public string TaskTicket { get; set; } = string.Empty;

    [MaxLength(500)]
    public string TaskDescription { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Status { get; set; } = "Not Started";

    [Range(0, 24)]
    public double HoursWorked { get; set; }

    [Range(0, 24)]
    public double ExtraHours { get; set; }

    [MaxLength(500)]
    public string Blockers { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public static readonly string[] StatusOptions =
    {
        "Not Started", "In Progress", "In Review",
        "Blocked", "Completed", "Carry Forward"
    };
}
