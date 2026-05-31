using bd2_projekt.Models;
using bd2_projekt.Data;
using MongoDB.Driver;

namespace bd2_projekt.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly IMongoCollection<AuditLog> _auditLogCollection;

    public AuditLogRepository(IMongoClient mongoClient, MongoAuditOptions mongoAuditOptions)
    {
        var database = mongoClient.GetDatabase(mongoAuditOptions.DatabaseName);
        _auditLogCollection = database.GetCollection<AuditLog>(mongoAuditOptions.CollectionName);
    }

    public async Task<AuditLog> CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _auditLogCollection.InsertOneAsync(auditLog, cancellationToken: cancellationToken);
        return auditLog;
    }

    public async Task<IReadOnlyList<AuditLog>> GetLatestAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _auditLogCollection
            .Find(_ => true)
            .SortByDescending(log => log.CreatedAtUtc)
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteByIdAsync(string auditLogId, CancellationToken cancellationToken = default)
    {
        var deleteResult = await _auditLogCollection.DeleteOneAsync(
            log => log.AuditLogId == auditLogId,
            cancellationToken);

        return deleteResult.DeletedCount > 0;
    }
}
