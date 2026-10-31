using Frontend_MarikinaAlert.Models;

namespace Frontend_MarikinaAlert.Repositories
{
    public interface IAgencyRepository
    {
        /// <summary>All agencies in display order, inactive ones included.</summary>
        Task<IReadOnlyList<Agency>> GetAllAsync();

        Task<Agency?> GetByIdAsync(int id);
        Task AddAsync(Agency agency);
        Task UpdateAsync(Agency agency);
    }
}
