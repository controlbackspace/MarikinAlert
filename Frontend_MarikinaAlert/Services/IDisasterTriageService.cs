using Frontend_MarikinaAlert.Models;

namespace Frontend_MarikinaAlert.Services
{
    public record TriageResult(ReportCategory Category, ReportPriority Priority, IReadOnlyList<string> MatchedKeywords);

    public interface IDisasterTriageService
    {
        /// <param name="rawMessage">The resident's free-text Taglish message.</param>
        /// <param name="selectedCategory">Optional category the resident tapped; used only when no keyword matches.</param>
        TriageResult Triage(string rawMessage, ReportCategory? selectedCategory = null);
    }
}
