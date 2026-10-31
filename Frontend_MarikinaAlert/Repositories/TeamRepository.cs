using Frontend_MarikinaAlert.Data;
using Frontend_MarikinaAlert.Models;
using Microsoft.EntityFrameworkCore;

namespace Frontend_MarikinaAlert.Repositories
{
    public class TeamRepository(AppDbContext db) : ITeamRepository
    {
        public async Task<IReadOnlyList<ResponseTeam>> GetAllAsync() =>
            await db.ResponseTeams.AsNoTracking().OrderBy(t => t.AgencyName).ThenBy(t => t.Name).ToListAsync();

        public Task<ResponseTeam?> GetByIdAsync(int id) =>
            db.ResponseTeams.FirstOrDefaultAsync(t => t.Id == id);

        public async Task AddAsync(ResponseTeam team)
        {
            team.StatusChangedAtUtc = DateTime.UtcNow;
            db.ResponseTeams.Add(team);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAsync(ResponseTeam team)
        {
            db.ResponseTeams.Update(team);
            await db.SaveChangesAsync();
        }

        public async Task ReleaseFromReportAsync(int reportId)
        {
            var teams = await db.ResponseTeams
                .Where(t => t.AssignedReportId == reportId
                         && (t.Status == TeamStatus.Dispatched || t.Status == TeamStatus.OnScene))
                .ToListAsync();
            foreach (var team in teams)
            {
                team.Status = TeamStatus.Returning;
                team.StatusChangedAtUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();
        }
    }
}
