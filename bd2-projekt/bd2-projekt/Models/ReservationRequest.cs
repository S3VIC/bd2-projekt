using System.ComponentModel.DataAnnotations;

namespace bd2_projekt.Models;

public sealed class ReservationRequest : IValidatableObject
{
    public static readonly IReadOnlyList<string> AllowedEventTypes =
    [
        "Conference",
        "Workshop",
        "Private Meeting"
    ];

    [Required]
    [StringLength(120, MinimumLength = 3)]
    [Display(Name = "Client full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(30)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required]
    [Display(Name = "Event type")]
    [RegularExpression("^(Conference|Workshop|Private Meeting)$",
        ErrorMessage = "Event type must be one of: Conference, Workshop, Private Meeting.")]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 4)]
    [DataType(DataType.Password)]
    [Display(Name = "Access password")]
    public string AccessPassword { get; set; } = string.Empty;

    public ReservationRequest Normalize() => new()
    {
        FullName = FullName.Trim(),
        Email = Email.Trim(),
        PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
        EventType = EventType.Trim(),
        AccessPassword = AccessPassword.Trim()
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(FullName))
        {
            yield return new ValidationResult("Client full name is required.", [nameof(FullName)]);
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult("Email address is required.", [nameof(Email)]);
        }

        if (string.IsNullOrWhiteSpace(EventType))
        {
            yield return new ValidationResult("Event type is required.", [nameof(EventType)]);
        }

        if (string.IsNullOrWhiteSpace(AccessPassword))
        {
            yield return new ValidationResult("Access password is required.", [nameof(AccessPassword)]);
        }
    }
}
