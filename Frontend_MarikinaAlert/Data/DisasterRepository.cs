using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Frontend_MarikinaAlert.Models;

namespace Frontend_MarikinaAlert.Data
{
    // The "Worker" that performs the actions
    public class DisasterRepository : IDisasterRepository
    {
        private readonly AppDbContext _context;

        public DisasterRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(DisasterReport report)
        {
            await _context.DisasterReports.AddAsync(report);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<DisasterReport>> GetAllAsync()
        {
            return await _context.DisasterReports.ToListAsync();
        }

        public async Task<DisasterReport?> GetByIdAsync(Guid id)
        {
            return await _context.DisasterReports.FindAsync(id);
        }
    }
}