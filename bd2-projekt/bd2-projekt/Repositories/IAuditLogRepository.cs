using bd2_projekt.Models;

namespace bd2_projekt.Repositories;

public interface IAuditLogRepository
{
    Task<AuditLog> CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetLatestAsync(int limit, CancellationToken cancellationToken = default);
    Task<bool> DeleteByIdAsync(string auditLogId, CancellationToken cancellationToken = default);
}
