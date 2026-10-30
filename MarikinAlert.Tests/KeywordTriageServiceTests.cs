using Frontend_MarikinaAlert.Models;
using Frontend_MarikinaAlert.Services;

namespace MarikinAlert.Tests
{
    public class KeywordTriageServiceTests
    {
        private readonly KeywordTriageService _triage = new();

        [TestCase("Tulong! May nasusunog na bahay dito sa kanto", ReportCategory.Fire, ReportPriority.High)]
        [TestCase("May sunog, may naipit sa loob!", ReportCategory.Fire, ReportPriority.Critical)]
        [TestCase("Gumuhong pader sa may elementary school", ReportCategory.Collapse, ReportPriority.Critical)]
        [TestCase("Naipit po kami sa guho", ReportCategory.Collapse, ReportPriority.Critical)]
        [TestCase("Tulong! May patay sa tabing ilog", ReportCategory.Medical, ReportPriority.Critical)]
        [TestCase("Lolo ko hindi humihinga", ReportCategory.Medical, ReportPriority.Critical)]
        [TestCase("May sugatan sa aksidente", ReportCategory.Medical, ReportPriority.High)]
        [TestCase("Bumabaha na sa kalye namin", ReportCategory.Flood, ReportPriority.High)]
        [TestCase("Baha na, nasa bubong na kami", ReportCategory.Flood, ReportPriority.Critical)]
        [TestCase("FIRE near the market, people inside", ReportCategory.Fire, ReportPriority.Critical)]
        [TestCase("May nasusunog na bahay, may bata pa sa loob", ReportCategory.Fire, ReportPriority.Critical)]
        public void Classifies_taglish_reports(string message, ReportCategory category, ReportPriority priority)
        {
            var result = _triage.Triage(message);

            Assert.That(result.Category, Is.EqualTo(category));
            Assert.That(result.Priority, Is.EqualTo(priority));
            Assert.That(result.MatchedKeywords, Is.Not.Empty);
        }

        [Test]
        public void Bahay_is_not_mistaken_for_baha()
        {
            var result = _triage.Triage("Nasa bahay lang po kami");

            Assert.That(result.Category, Is.EqualTo(ReportCategory.Unclassified));
            Assert.That(result.Priority, Is.EqualTo(ReportPriority.Normal));
        }

        [Test]
        public void Accents_and_capitals_are_ignored()
        {
            var result = _triage.Triage("SUNÓG SA KANTO");

            Assert.That(result.Category, Is.EqualTo(ReportCategory.Fire));
        }

        [Test]
        public void Uses_quick_pick_only_when_no_keyword_matches()
        {
            var noKeywords = _triage.Triage("Tulong po bilisan", ReportCategory.Flood);
            Assert.That(noKeywords.Category, Is.EqualTo(ReportCategory.Flood));
            Assert.That(noKeywords.Priority, Is.EqualTo(ReportPriority.High));

            var keywordsWin = _triage.Triage("May sunog dito", ReportCategory.Flood);
            Assert.That(keywordsWin.Category, Is.EqualTo(ReportCategory.Fire));
        }

        [Test]
        public void Trapped_without_other_category_is_treated_as_collapse()
        {
            var result = _triage.Triage("Trapped kami dito sa second floor");

            Assert.That(result.Category, Is.EqualTo(ReportCategory.Collapse));
            Assert.That(result.Priority, Is.EqualTo(ReportPriority.Critical));
        }
    }
}
