using Frontend_MarikinaAlert.Data;
using Frontend_MarikinaAlert.Models;
using Microsoft.EntityFrameworkCore;

namespace Frontend_MarikinaAlert.Repositories
{
    public class AgencyRepository(AppDbContext db) : IAgencyRepository
    {
        public async Task<IReadOnlyList<Agency>> GetAllAsync() =>
            await db.Agencies.AsNoTracking().OrderBy(a => a.SortOrder).ThenBy(a => a.Name).ToListAsync();

        public Task<Agency?> GetByIdAsync(int id) =>
            db.Agencies.FirstOrDefaultAsync(a => a.Id == id);

        public async Task AddAsync(Agency agency)
        {
            // New agencies go to the bottom of the list.
            agency.SortOrder = (await db.Agencies.MaxAsync(a => (int?)a.SortOrder) ?? -1) + 1;
            db.Agencies.Add(agency);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Agency agency)
        {
            db.Agencies.Update(agency);
            await db.SaveChangesAsync();
        }
    }
}
