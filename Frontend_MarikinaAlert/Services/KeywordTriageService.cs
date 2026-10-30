using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Frontend_MarikinaAlert.Models;

namespace Frontend_MarikinaAlert.Services
{
    /// <summary>
    /// Rule-based Taglish triage. Each rule is a regex over the normalized message
    /// (lowercase, no accents). Patterns use stems so Tagalog affixes still match,
    /// e.g. "sunog" matches "nasusunog" and "sinunog".
    /// </summary>
    public partial class KeywordTriageService : IDisasterTriageService
    {
        private sealed record Rule(ReportCategory Category, ReportPriority Priority, Regex Pattern);

        private static Rule R(ReportCategory c, ReportPriority p, string pattern) =>
            new(c, p, new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant));

        private static readonly Rule[] CategoryRules =
        [
            // Fire
            R(ReportCategory.Fire, ReportPriority.High, @"sunog|apoy|\bfire\b|usok|smoke|burning|nasusunog|\bgas leak|sumabog|explosion"),

            // Structural collapse (always life-threatening)
            R(ReportCategory.Collapse, ReportPriority.Critical, @"gumuho|\bguho|nagiba|\bgiba|bumagsak na (bahay|pader|building|gusali)|collapse|landslide|natabunan|nadaganan"),

            // Medical
            R(ReportCategory.Medical, ReportPriority.Critical, @"\bpatay\b|namatay|walang malay|unconscious|(hindi|di) (na )?(humihinga|makahinga)|not breathing|can'?t breathe|nalunod|duguan|bleeding|heart attack|inatake|atake sa puso|stroke|nahimatay|seizure|kinukumbulsyon"),
            R(ReportCategory.Medical, ReportPriority.High, @"sugatan|nasugatan|injur|\bdugo\b|aksidente|accident|nabundol|nasagasaan|manganganak|\blabor\b|lagnat|sakit|medical|ambulan"),

            // Flood (\bbaha\b so "bahay" = house does not match)
            R(ReportCategory.Flood, ReportPriority.High, @"\bbaha\b|bumabaha|binaha|binabaha|\bflood|umaapaw|apaw|lubog|lumulubog|tumataas ang tubig|rising water"),
        ];

        // Boosters raise any report to Critical. If no category matched, trapped people imply Collapse.
        private static readonly Regex TrappedPattern = new(
            @"naipit|\bipit\b|trapped|\btrap\b|stuck|(hindi|di) (kami |ako |sila )?makalabas|nakulong",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex LifeThreatPattern = new(
            @"nalulunod|drowning|sa bubong|on the roof|rooftop|stranded|(bata|tao|matanda|lola|lolo) (pa )?sa loob|people inside|someone inside",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public TriageResult Triage(string rawMessage, ReportCategory? selectedCategory = null)
        {
            var text = Normalize(rawMessage);
            var matched = new List<string>();
            var scores = new Dictionary<ReportCategory, (int Hits, ReportPriority Priority)>();
            var priority = ReportPriority.Normal;

            foreach (var rule in CategoryRules)
            {
                var hits = rule.Pattern.Matches(text);
                if (hits.Count == 0) continue;

                var current = scores.GetValueOrDefault(rule.Category);
                scores[rule.Category] = (current.Hits + hits.Count, rule.Priority > current.Priority ? rule.Priority : current.Priority);
                if (rule.Priority > priority) priority = rule.Priority;
                matched.AddRange(hits.Select(h => h.Value));
            }

            var category = scores.Count == 0
                ? ReportCategory.Unclassified
                // Most hits wins; ties go to the category with the more urgent match.
                : scores.OrderByDescending(s => s.Value.Hits).ThenByDescending(s => s.Value.Priority).First().Key;

            var trapped = TrappedPattern.Matches(text);
            if (trapped.Count > 0)
            {
                priority = ReportPriority.Critical;
                if (category == ReportCategory.Unclassified) category = ReportCategory.Collapse;
                matched.AddRange(trapped.Select(h => h.Value));
            }

            var lifeThreat = LifeThreatPattern.Matches(text);
            if (lifeThreat.Count > 0)
            {
                priority = ReportPriority.Critical;
                matched.AddRange(lifeThreat.Select(h => h.Value));
            }

            // Nothing recognized: trust the resident's quick-pick, but never above High without evidence.
            if (category == ReportCategory.Unclassified && selectedCategory is { } picked && picked != ReportCategory.Unclassified)
            {
                category = picked;
                if (priority < ReportPriority.High) priority = ReportPriority.High;
            }

            return new TriageResult(category, priority, matched.Distinct().ToList());
        }

        private static string Normalize(string input)
        {
            var decomposed = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            return Whitespace().Replace(sb.ToString(), " ").Trim();
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex Whitespace();
    }
}
