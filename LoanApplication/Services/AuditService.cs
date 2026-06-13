using LoanApplication.Data;
using LoanApplication.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace LoanApplication.Services
{
    public interface IAuditService
    {
        Task LogAction(int userId, string action, string entityType, int? entityId, object oldValues, object newValues, string reason = "null");
        Task<List<AuditLog>> GetAuditLogs(int? userId = null, string entityType = null, DateTime? fromDate = null, DateTime? toDate = null);
    }


    public class AuditService : IAuditService
    {
        private readonly LoanManagementDbContext _context;

        public AuditService(LoanManagementDbContext context)
        {
            _context = context;
        }

        public async Task LogAction(int userId, string action, string entityType, int? entityId, object oldValues, object newValues, string reason = "null")
        {
            try
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    OldValues = oldValues != null ? System.Text.Json.JsonSerializer.Serialize(oldValues) : "null",
                    NewValues = newValues != null ? System.Text.Json.JsonSerializer.Serialize(newValues) : "null",
                    Reason = reason,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error " + ex.Message);
                throw;
            }
        }

        public async Task<List<AuditLog>> GetAuditLogs(int? userId = null, string entityType = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.AuditLogs.Include(a => a.User).AsQueryable();

            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(entityType))
                query = query.Where(a => a.EntityType == entityType);

            if (fromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(a => a.CreatedAt <= toDate.Value);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(1000)
                .ToListAsync();
        }
    }
}
