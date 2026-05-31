using System.ComponentModel.DataAnnotations;

namespace bd2_projekt.Models;

public sealed class AuditLogRequest : IValidatableObject
{
    public static readonly IReadOnlyList<string> AllowedOutcomes =
    [
        "registration_success",
        "registration_error",
        "client_validation_error",
        "api_unreachable"
    ];

    [Required]
    [StringLength(40)]
    public string Outcome { get; set; } = string.Empty;

    [StringLength(40)]
    public string? EventType { get; set; }

    [EmailAddress]
    [StringLength(254)]
    public string? Email { get; set; }

    public Guid? ReservationId { get; set; }

    [Range(100, 599)]
    public int? HttpStatusCode { get; set; }

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? PayloadJson { get; set; }

    [StringLength(40)]
    public string? Source { get; set; }

    public AuditLogRequest Normalize() => new()
    {
        Outcome = Outcome.Trim(),
        EventType = string.IsNullOrWhiteSpace(EventType) ? null : EventType.Trim(),
        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
        ReservationId = ReservationId,
        HttpStatusCode = HttpStatusCode,
        Message = Message.Trim(),
        PayloadJson = string.IsNullOrWhiteSpace(PayloadJson) ? null : PayloadJson.Trim(),
        Source = string.IsNullOrWhiteSpace(Source) ? "webapp" : Source.Trim()
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Outcome))
        {
            yield return new ValidationResult("Outcome is required.", [nameof(Outcome)]);
            yield break;
        }

        if (!AllowedOutcomes.Contains(Outcome, StringComparer.Ordinal))
        {
            yield return new ValidationResult(
                "Outcome must be one of: registration_success, registration_error, client_validation_error, api_unreachable.",
                [nameof(Outcome)]);
        }
    }
}
