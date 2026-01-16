using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Frontend_MarikinaAlert.Models;

namespace Frontend_MarikinaAlert.Data
{
    // The "Menu" of available actions
    public interface IDisasterRepository
    {
        Task AddAsync(DisasterReport report);
        Task<IEnumerable<DisasterReport>> GetAllAsync();
        Task<DisasterReport?> GetByIdAsync(Guid id); // Note: We use Guid here now!
    }
}