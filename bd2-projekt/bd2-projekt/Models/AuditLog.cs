using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace bd2_projekt.Models;

public sealed class AuditLog
{
    [Key]
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string AuditLogId { get; set; } = Guid.NewGuid().ToString("D");

    [Required]
    [StringLength(40)]
    public string Outcome { get; set; } = string.Empty;

    [StringLength(40)]
    public string? EventType { get; set; }

    [EmailAddress]
    [StringLength(254)]
    public string? Email { get; set; }

    [BsonRepresentation(BsonType.String)]
    public string? ReservationId { get; set; }

    [Range(100, 599)]
    public int? HttpStatusCode { get; set; }

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? PayloadJson { get; set; }

    [Required]
    [StringLength(40)]
    public string Source { get; set; } = "webapp";

    [Required]
    public DateTimeOffset CreatedAtUtc { get; set; }
}
