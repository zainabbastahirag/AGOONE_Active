using System.ComponentModel.DataAnnotations;

namespace TeamTracker.Models;

public class Note
{
    public int Id { get; set; }

    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;

    [Required, MaxLength(3000)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Type { get; set; } = "Note";

    [MaxLength(100)]
    public string Author { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static readonly string[] Types = { "Note", "Review", "Achievement", "Concern", "Goal", "Chat" };
}
