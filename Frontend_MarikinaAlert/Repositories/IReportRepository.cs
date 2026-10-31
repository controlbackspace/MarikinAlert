using Frontend_MarikinaAlert.Models;

namespace Frontend_MarikinaAlert.Repositories
{
    public interface IReportRepository
    {
        Task<DisasterReport> AddAsync(DisasterReport report);
        Task<DisasterReport?> GetByIdAsync(int id);
        Task<DisasterReport?> GetByTrackingCodeAsync(string code);
        Task UpdateAsync(DisasterReport report);

        /// <summary>Active, Verified and Relayed reports, most urgent first.</summary>
        Task<IReadOnlyList<DisasterReport>> GetOpenAsync();

        /// <summary>Resolved and Dismissed reports, newest first.</summary>
        Task<IReadOnlyList<DisasterReport>> GetClosedAsync(ReportStatus? status, string? search, int take = 200);

        Task AddContactAsync(ContactAttempt attempt);

        /// <summary>Call and radio attempts for the given reports, oldest first.</summary>
        Task<IReadOnlyList<ContactAttempt>> GetContactsAsync(IReadOnlyCollection<int> reportIds);
    }
}
