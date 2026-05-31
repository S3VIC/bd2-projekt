using bd2_projekt.Models;
using Microsoft.EntityFrameworkCore;

namespace bd2_projekt.Data;

public sealed class ReservationDbContext(DbContextOptions<ReservationDbContext> options) : DbContext(options)
{
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var reservation = modelBuilder.Entity<Reservation>();
        reservation.ToTable("reservations");
        reservation.HasKey(entity => entity.ReservationId);

        reservation.Property(entity => entity.ReservationId).HasColumnName("reservation_id");
        reservation.Property(entity => entity.FullName).HasColumnName("full_name").HasMaxLength(120).IsRequired();
        reservation.Property(entity => entity.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        reservation.Property(entity => entity.PhoneNumber).HasColumnName("phone_number").HasMaxLength(30);
        reservation.Property(entity => entity.EventType).HasColumnName("event_type").HasMaxLength(40).IsRequired();
        reservation.Property(entity => entity.SubmittedAtUtc).HasColumnName("submitted_at_utc").IsRequired();

        reservation.HasIndex(entity => entity.SubmittedAtUtc);
        reservation.HasIndex(entity => entity.Email);
    }
}
