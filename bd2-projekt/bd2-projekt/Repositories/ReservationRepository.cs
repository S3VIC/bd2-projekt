using bd2_projekt.Data;
using bd2_projekt.Models;
using Microsoft.EntityFrameworkCore;

namespace bd2_projekt.Repositories;

public sealed class ReservationRepository(ReservationDbContext dbContext) : IReservationRepository
{
    public async Task<Reservation> CreateAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return reservation;
    }

    public async Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Reservations
            .AsNoTracking()
            .OrderByDescending(reservation => reservation.SubmittedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<Reservation?> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken = default) =>
        await dbContext.Reservations
            .AsNoTracking()
            .FirstOrDefaultAsync(reservation => reservation.ReservationId == reservationId, cancellationToken);
}
