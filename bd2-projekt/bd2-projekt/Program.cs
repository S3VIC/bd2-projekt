using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using bd2_projekt.Data;
using bd2_projekt.Models;
using bd2_projekt.Repositories;
using Microsoft.EntityFrameworkCore;

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

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddRazorPages();
builder.Services.AddDbContext<ReservationDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.Parse(serverVersion));
});
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();

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