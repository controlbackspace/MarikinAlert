using Frontend_MarikinaAlert.Models;

namespace Frontend_MarikinaAlert.Repositories
{
    public interface ITeamRepository
    {
        /// <summary>All teams, by agency then name.</summary>
        Task<IReadOnlyList<ResponseTeam>> GetAllAsync();

        Task<ResponseTeam?> GetByIdAsync(int id);
        Task AddAsync(ResponseTeam team);
        Task UpdateAsync(ResponseTeam team);

        /// <summary>Teams still dispatched to or on scene at this report start heading back.</summary>
        Task ReleaseFromReportAsync(int reportId);
    }
}
