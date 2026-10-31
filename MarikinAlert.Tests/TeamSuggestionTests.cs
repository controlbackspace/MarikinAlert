using Frontend_MarikinaAlert.Models;
using Frontend_MarikinaAlert.Services;

namespace MarikinAlert.Tests
{
    public class TeamSuggestionTests
    {
        private static ResponseTeam Team(int id, string name, string agency, string barangay,
            TeamStatus status = TeamStatus.Available) => new()
        {
            Id = id,
            Name = name,
            AgencyName = agency,
            BaseBarangay = barangay,
            Status = status
        };

        private static DisasterReport Report(string barangay, ReportCategory category) => new()
        {
            Id = 1,
            Barangay = barangay,
            Category = category,
            RawMessage = "test"
        };

        [Test]
        public void Map_has_every_barangay_once_with_no_overlapping_tiles()
        {
            Assert.That(Marikina.Map.Keys, Is.EquivalentTo(Marikina.Barangays));
            Assert.That(Marikina.Map.Values.Select(t => (t.Col, t.Row)), Is.Unique);
            Assert.That(Marikina.Map.Values.All(t => t.Col is >= 0 and < 4 && t.Row is >= 0 and < 5), Is.True,
                "the dashboard draws a 4 x 5 grid");
        }

        [Test]
        public void Right_agency_comes_before_a_closer_team_from_another_agency()
        {
            var report = Report("Concepcion Uno", ReportCategory.Fire);
            var ranked = IncidentInsights.RankTeams(
            [
                Team(1, "Rescue 1", "Marikina Rescue 161 / Ambulance", "Concepcion Uno"),
                Team(2, "BFP Engine 1", "BFP Marikina", "Barangka"),
            ], report, Marikina.SuggestedAgency(report.Category));

            Assert.That(ranked.Select(t => t.Id), Is.EqualTo(new[] { 2, 1 }));
        }

        [Test]
        public void Among_the_same_agency_the_closest_base_comes_first()
        {
            var report = Report("Marikina Heights", ReportCategory.Fire);
            var ranked = IncidentInsights.RankTeams(
            [
                Team(1, "BFP Engine 1", "BFP Marikina", "Barangka"),
                Team(2, "BFP Engine 2", "BFP Marikina", "Fortune"),
            ], report, "BFP Marikina");

            Assert.That(ranked.Select(t => t.Id), Is.EqualTo(new[] { 2, 1 }));
        }

        [Test]
        public void Busy_and_out_of_service_teams_are_not_offered()
        {
            var ranked = IncidentInsights.RankTeams(
            [
                Team(1, "A", "BFP Marikina", "Tumana", TeamStatus.Dispatched),
                Team(2, "B", "BFP Marikina", "Tumana", TeamStatus.OnScene),
                Team(3, "C", "BFP Marikina", "Tumana", TeamStatus.Returning),
                Team(4, "D", "BFP Marikina", "Tumana", TeamStatus.OutOfService),
                Team(5, "E", "BFP Marikina", "Tumana"),
            ], Report("Tumana", ReportCategory.Fire), "BFP Marikina");

            Assert.That(ranked.Select(t => t.Id), Is.EqualTo(new[] { 5 }));
        }
    }
}
