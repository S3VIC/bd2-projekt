using System.ComponentModel.DataAnnotations;

namespace bd2_projekt.Models;

public sealed class Reservation
{
    [Key]
    public Guid ReservationId { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(120, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [Required]
    [RegularExpression("^(Conference|Workshop|Private Meeting)$")]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset SubmittedAtUtc { get; set; }
}
