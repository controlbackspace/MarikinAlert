using Frontend_MarikinaAlert.Models;
using Frontend_MarikinaAlert.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Frontend_MarikinaAlert.Services
{
    public class TemporaryTriageService : IDisasterTriageService
    {
        private readonly IDisasterRepository _repository;

        public TemporaryTriageService(IDisasterRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<DisasterReport>> GetAllReportsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<DisasterReport> GetReportByIdAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        // CHANGE: Added 'contactNumber' here
        public async Task<DisasterReport> TriageAndAnalyzeAsync(string rawMessage, string name, string contactNumber, string location)
        {
            var report = new DisasterReport
            {
                Id = Guid.NewGuid(),
                OriginNodeId = "My_Laptop_Node_A",
                SenderName = name,

                // CHANGE: Map the incoming number to the Database Object
                ContactNumber = contactNumber,

                RawMessage = rawMessage,
                Location = location,
                Category = ReportCategory.Infrastructure,
                Priority = ReportPriority.Medium,
                IsVerified = false,
                Timestamp = DateTime.Now
            };

            await _repository.AddAsync(report);
            return report;
        }
    }
}