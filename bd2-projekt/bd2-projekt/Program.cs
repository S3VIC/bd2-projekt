using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using bd2_projekt.Data;
using bd2_projekt.Models;
using bd2_projekt.Repositories;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("MariaDb")
    ?? builder.Configuration.GetConnectionString("MySql");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'ConnectionStrings:MariaDb' is missing. " +
        "Set it via environment variables or dotnet user-secrets.");
}
var serverVersion = builder.Configuration["MariaDbServerVersion"]
    ?? builder.Configuration["MySqlServerVersion"]
    ?? "11.4.0-mariadb";
var sharedAccessPassword = builder.Configuration["SharedAccessPassword"];
if (string.IsNullOrWhiteSpace(sharedAccessPassword))
{
    throw new InvalidOperationException(
        "Configuration key 'SharedAccessPassword' is missing. " +
        "Set it via environment variables or dotnet user-secrets.");
}
var mongoConnectionString = builder.Configuration["Mongo:ConnectionString"];
if (string.IsNullOrWhiteSpace(mongoConnectionString))
{
    throw new InvalidOperationException(
        "Configuration key 'Mongo:ConnectionString' is missing. " +
        "Set it via environment variables or dotnet user-secrets.");
}
var mongoDatabaseName = builder.Configuration["Mongo:DatabaseName"] ?? "event_audit";
var mongoCollectionName = builder.Configuration["Mongo:CollectionName"] ?? "registration_logs";

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<ReservationDbContext>(options =>
{
    options.UseMySql(connectionString, Microsoft.EntityFrameworkCore.ServerVersion.Parse(serverVersion));
});
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(new MongoAuditOptions(mongoDatabaseName, mongoCollectionName));
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ReservationDbContext>();
    dbContext.Database.Migrate();
}

app.MapPost("/api/reservations", async (
        ReservationRequest request,
        IReservationRepository reservationRepository,
        CancellationToken cancellationToken) =>
    {
        var normalizedRequest = request.Normalize();
        var validationErrors = ValidateReservationRequest(normalizedRequest);
        if (validationErrors is not null)
        {
            return Results.ValidationProblem(validationErrors);
        }

        if (!PasswordMatches(normalizedRequest.AccessPassword, sharedAccessPassword))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(ReservationRequest.AccessPassword)] = ["Shared password is invalid."]
            });
        }

        var reservation = new Reservation
        {
            FullName = normalizedRequest.FullName,
            Email = normalizedRequest.Email,
            PhoneNumber = normalizedRequest.PhoneNumber,
            EventType = normalizedRequest.EventType,
            SubmittedAtUtc = DateTimeOffset.UtcNow
        };

        var createdReservation = await reservationRepository.CreateAsync(reservation, cancellationToken);
        var response = MapToResponse(createdReservation);
        return Results.Created($"/api/reservations/{response.ReservationId}", response);
    })
    .WithName("CreateReservation");

app.MapPost("/api/audit-logs", async (
        AuditLogRequest request,
        IAuditLogRepository auditLogRepository,
        CancellationToken cancellationToken) =>
    {
        var normalizedRequest = request.Normalize();
        var validationErrors = ValidateAuditLogRequest(normalizedRequest);
        if (validationErrors is not null)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var auditLog = new AuditLog
        {
            Outcome = normalizedRequest.Outcome,
            EventType = normalizedRequest.EventType,
            Email = normalizedRequest.Email,
            ReservationId = normalizedRequest.ReservationId?.ToString("D"),
            HttpStatusCode = normalizedRequest.HttpStatusCode,
            Message = normalizedRequest.Message,
            PayloadJson = normalizedRequest.PayloadJson,
            Source = normalizedRequest.Source ?? "webapp",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var createdAuditLog = await auditLogRepository.CreateAsync(auditLog, cancellationToken);
        return Results.Accepted($"/api/audit-logs/{createdAuditLog.AuditLogId}", new { createdAuditLog.AuditLogId });
    })
    .WithName("CreateAuditLog");

app.MapGet("/api/audit-logs", async (
        int? limit,
        IAuditLogRepository auditLogRepository,
        CancellationToken cancellationToken) =>
    {
        var resolvedLimit = limit ?? 100;
        if (resolvedLimit is < 1 or > 500)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["limit"] = ["Limit must be between 1 and 500."]
            });
        }

        var logs = await auditLogRepository.GetLatestAsync(resolvedLimit, cancellationToken);
        return Results.Ok(logs);
    })
    .WithName("GetAuditLogs");

app.MapDelete("/api/audit-logs/{auditLogId}", async (
        string auditLogId,
        IAuditLogRepository auditLogRepository,
        CancellationToken cancellationToken) =>
    {
        var normalizedAuditLogId = auditLogId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedAuditLogId))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["auditLogId"] = ["Audit log ID is required."]
            });
        }

        var deleted = await auditLogRepository.DeleteByIdAsync(normalizedAuditLogId, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    })
    .WithName("DeleteAuditLogById");

app.MapGet("/api/reservations", async (
        IReservationRepository reservationRepository,
        CancellationToken cancellationToken) =>
    {
        var reservations = await reservationRepository.GetAllAsync(cancellationToken);
        var responses = reservations.Select(MapToResponse);
        return Results.Ok(responses);
    })
    .WithName("GetReservations");

app.MapGet("/api/reservations/{reservationId:guid}", async (
        Guid reservationId,
        IReservationRepository reservationRepository,
        CancellationToken cancellationToken) =>
    {
        var reservation = await reservationRepository.GetByIdAsync(reservationId, cancellationToken);
        return reservation is null ? Results.NotFound() : Results.Ok(MapToResponse(reservation));
    })
    .WithName("GetReservationById");

app.MapRazorPages();

app.Run();

static Dictionary<string, string[]>? ValidateReservationRequest(ReservationRequest request)
{
    var validationContext = new ValidationContext(request);
    var validationResults = new List<ValidationResult>();

    if (Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
    {
        return null;
    }

    return validationResults
        .SelectMany(result =>
            result.MemberNames.Any()
                ? result.MemberNames.Select(member => new { Member = member, result.ErrorMessage })
                : [new { Member = "request", result.ErrorMessage }])
        .GroupBy(entry => entry.Member)
        .ToDictionary(
            group => group.Key,
            group => group
                .Select(entry => entry.ErrorMessage ?? "Invalid value.")
                .Distinct()
                .ToArray());
}

static Dictionary<string, string[]>? ValidateAuditLogRequest(AuditLogRequest request)
{
    var validationContext = new ValidationContext(request);
    var validationResults = new List<ValidationResult>();

    if (Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
    {
        return null;
    }

    return validationResults
        .SelectMany(result =>
            result.MemberNames.Any()
                ? result.MemberNames.Select(member => new { Member = member, result.ErrorMessage })
                : [new { Member = "request", result.ErrorMessage }])
        .GroupBy(entry => entry.Member)
        .ToDictionary(
            group => group.Key,
            group => group
                .Select(entry => entry.ErrorMessage ?? "Invalid value.")
                .Distinct()
                .ToArray());
}

static ReservationResponse MapToResponse(Reservation reservation) =>
    new(
        reservation.ReservationId,
        reservation.FullName,
        reservation.Email,
        reservation.PhoneNumber,
        reservation.EventType,
        reservation.SubmittedAtUtc);

static bool PasswordMatches(string providedPassword, string expectedPassword)
{
    var providedBytes = Encoding.UTF8.GetBytes(providedPassword);
    var expectedBytes = Encoding.UTF8.GetBytes(expectedPassword);
    return providedBytes.Length == expectedBytes.Length &&
           CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
}

record ReservationResponse(
    Guid ReservationId,
    string FullName,
    string Email,
    string? PhoneNumber,
    string EventType,
    DateTimeOffset SubmittedAtUtc);