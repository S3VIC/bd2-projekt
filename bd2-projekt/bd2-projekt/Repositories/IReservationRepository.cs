using bd2_projekt.Models;

namespace bd2_projekt.Repositories;

public interface IReservationRepository
{
    Task<Reservation> CreateAsync(Reservation reservation, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Reservation?> GetByIdAsync(Guid reservationId, CancellationToken cancellationToken = default);
}
