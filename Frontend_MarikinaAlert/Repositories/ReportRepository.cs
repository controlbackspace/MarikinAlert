using Frontend_MarikinaAlert.Data;
using Frontend_MarikinaAlert.Models;
using Microsoft.EntityFrameworkCore;

namespace Frontend_MarikinaAlert.Repositories
{
    public class ReportRepository(AppDbContext db) : IReportRepository
    {
        private static readonly ReportStatus[] OpenStatuses = [ReportStatus.Active, ReportStatus.Verified, ReportStatus.Relayed];
        private static readonly ReportStatus[] ClosedStatuses = [ReportStatus.Resolved, ReportStatus.Dismissed];

        public async Task<DisasterReport> AddAsync(DisasterReport report)
        {
            report.CreatedAtUtc = DateTime.UtcNow;
            db.DisasterReports.Add(report);
            await db.SaveChangesAsync();
            return report;
        }

        public Task<DisasterReport?> GetByIdAsync(int id) =>
            db.DisasterReports.FirstOrDefaultAsync(r => r.Id == id);

        public async Task UpdateAsync(DisasterReport report)
        {
            report.UpdatedAtUtc = DateTime.UtcNow;
            db.DisasterReports.Update(report);
            await db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<DisasterReport>> GetOpenAsync(ReportCategory? category, ReportStatus? status)
        {
            var query = db.DisasterReports.AsNoTracking().Where(r => OpenStatuses.Contains(r.Status));

            if (category is not null) query = query.Where(r => r.Category == category);
            if (status is not null) query = query.Where(r => r.Status == status);

            // Life-threatening first, then untouched (Active) before handled ones, then newest.
            return await query
                .OrderByDescending(r => EF.Property<int>(r, "PriorityRank"))
                .ThenBy(r => r.Status == ReportStatus.Active ? 0 : 1)
                .ThenByDescending(r => r.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<DisasterReport>> GetClosedAsync(ReportStatus? status, string? search, int take = 200)
        {
            var query = db.DisasterReports.AsNoTracking().Where(r => ClosedStatuses.Contains(r.Status));

            if (status is not null) query = query.Where(r => r.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = $"%{search.Trim()}%";
                query = query.Where(r =>
                    EF.Functions.Like(r.RawMessage, term) ||
                    EF.Functions.Like(r.Barangay, term) ||
                    EF.Functions.Like(r.Landmark!, term) ||
                    EF.Functions.Like(r.SenderName!, term));
            }

            return await query
                .OrderByDescending(r => r.UpdatedAtUtc ?? r.CreatedAtUtc)
                .Take(take)
                .ToListAsync();
        }

        public Task<int> CountOpenAsync(ReportPriority? priority = null)
        {
            var query = db.DisasterReports.Where(r => OpenStatuses.Contains(r.Status));
            if (priority is not null) query = query.Where(r => r.Priority == priority);
            return query.CountAsync();
        }
    }
}
