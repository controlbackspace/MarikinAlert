using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Frontend_MarikinaAlert.Models;

namespace Frontend_MarikinaAlert.Services
{
    public interface IDisasterTriageService
    {
        // CHANGE: Added 'string contactNumber' to the parameters
        Task<DisasterReport> TriageAndAnalyzeAsync(string rawMessage, string name, string contactNumber, string location);

        Task<IEnumerable<DisasterReport>> GetAllReportsAsync();
        Task<DisasterReport> GetReportByIdAsync(Guid id);
    }
}