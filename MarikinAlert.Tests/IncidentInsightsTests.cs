using Frontend_MarikinaAlert.Models;
using Frontend_MarikinaAlert.Services;

namespace MarikinAlert.Tests
{
    public class IncidentInsightsTests
    {
        private static readonly DateTime T0 = new(2026, 10, 31, 2, 0, 0, DateTimeKind.Utc);

        private static DisasterReport Report(int id, string barangay, ReportCategory category, int minutesAfterT0,
            ReportPriority priority = ReportPriority.High, ReportStatus status = ReportStatus.Active) => new()
        {
            Id = id,
            Barangay = barangay,
            Category = category,
            Priority = priority,
            Status = status,
            RawMessage = "test",
            CreatedAtUtc = T0.AddMinutes(minutesAfterT0)
        };

        [Test]
        public void Same_barangay_and_category_within_window_is_a_duplicate_of_the_first()
        {
            var dupes = IncidentInsights.FindDuplicates(
            [
                Report(1, "Tumana", ReportCategory.Fire, 0),
                Report(2, "Tumana", ReportCategory.Fire, 4),
                Report(3, "Tumana", ReportCategory.Fire, 12),
            ]);

            Assert.That(dupes, Does.Not.ContainKey(1));
            Assert.That(dupes[2], Is.EqualTo(1));
            Assert.That(dupes[3], Is.EqualTo(1), "chains back to the first report");
        }

        [Test]
        public void Different_barangay_category_or_far_apart_are_not_duplicates()
        {
            var dupes = IncidentInsights.FindDuplicates(
            [
                Report(1, "Tumana", ReportCategory.Fire, 0),
                Report(2, "Malanday", ReportCategory.Fire, 2),
                Report(3, "Tumana", ReportCategory.Flood, 3),
                Report(4, "Tumana", ReportCategory.Fire, 40),
            ]);

            Assert.That(dupes, Is.Empty);
        }

        [Test]
        public void Unclassified_reports_are_never_marked_as_duplicates()
        {
            var dupes = IncidentInsights.FindDuplicates(
            [
                Report(1, "Parang", ReportCategory.Unclassified, 0),
                Report(2, "Parang", ReportCategory.Unclassified, 1),
            ]);

            Assert.That(dupes, Is.Empty);
        }

        [Test]
        public void Barangay_match_ignores_case_and_spaces()
        {
            var dupes = IncidentInsights.FindDuplicates(
            [
                Report(1, "Santo Niño", ReportCategory.Medical, 0),
                Report(2, " santo niño ", ReportCategory.Medical, 5),
            ]);

            Assert.That(dupes[2], Is.EqualTo(1));
        }

        [TestCase(ReportPriority.Critical, 4, false)]
        [TestCase(ReportPriority.Critical, 5, true)]
        [TestCase(ReportPriority.High, 9, false)]
        [TestCase(ReportPriority.High, 10, true)]
        [TestCase(ReportPriority.Normal, 120, false)]
        public void New_reports_become_overdue_by_priority(ReportPriority priority, int minutesWaiting, bool expected)
        {
            var report = Report(1, "Tumana", ReportCategory.Fire, 0, priority);

            Assert.That(IncidentInsights.IsOverdue(report, T0.AddMinutes(minutesWaiting)), Is.EqualTo(expected));
        }

        [Test]
        public void Handled_reports_are_never_overdue()
        {
            var report = Report(1, "Tumana", ReportCategory.Fire, 0, ReportPriority.Critical, ReportStatus.Verified);

            Assert.That(IncidentInsights.IsOverdue(report, T0.AddHours(3)), Is.False);
        }
    }
}
