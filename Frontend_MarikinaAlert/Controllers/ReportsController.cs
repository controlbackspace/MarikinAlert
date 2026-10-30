using Frontend_MarikinaAlert.Localization;
using Frontend_MarikinaAlert.Models;
using Frontend_MarikinaAlert.Repositories;
using Frontend_MarikinaAlert.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Frontend_MarikinaAlert.Controllers
{
    // Public, resident-facing pages.
    public class ReportsController(IReportRepository reports, IDisasterTriageService triage, IStringLocalizer<Translations> T) : Controller
    {
        [HttpGet]
        public IActionResult Create() => View(new CreateReportViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReportViewModel model)
        {
            // Some keyboards send "ñ" decomposed (n + combining tilde); normalize before comparing.
            model.Barangay = model.Barangay?.Normalize(System.Text.NormalizationForm.FormC);
            if (model.Barangay is not null && !Marikina.Barangays.Contains(model.Barangay))
                ModelState.AddModelError(nameof(model.Barangay), T["Choose a barangay from the list."]);

            if (!ModelState.IsValid) return View(model);

            var result = triage.Triage(model.RawMessage!, model.SelectedCategory);

            var report = await reports.AddAsync(new DisasterReport
            {
                SenderName = Clean(model.SenderName),
                ContactNumber = Clean(model.ContactNumber),
                Barangay = model.Barangay!,
                Landmark = Clean(model.Landmark),
                RawMessage = model.RawMessage!.Trim(),
                Category = result.Category,
                Priority = result.Priority,
                MatchedKeywords = result.MatchedKeywords.Count > 0 ? string.Join(", ", result.MatchedKeywords) : null,
                Status = ReportStatus.Active,
                TrackingCode = await TrackingCodes.NewAsync(reports)
            });

            // Post/Redirect/Get so refreshing the confirmation page doesn't send a duplicate report.
            // The id travels in TempData (an encrypted cookie), not the URL, so only the browser that
            // sent the report can see its confirmation; ids are sequential and would otherwise leak
            // other residents' locations and phone numbers.
            TempData[SubmittedKey] = report.Id;
            return RedirectToAction(nameof(Submitted));
        }

        [HttpGet]
        public async Task<IActionResult> Submitted()
        {
            if (TempData.Peek(SubmittedKey) is not int id) return RedirectToAction(nameof(Create));

            var report = await reports.GetByIdAsync(id);
            return report is null ? RedirectToAction(nameof(Create)) : View(report);
        }

        // "Check my report": look a report up by its tracking code.
        [HttpGet]
        public async Task<IActionResult> Status(string? code)
        {
            var model = new ReportStatusViewModel { Code = code };
            var normalized = TrackingCodes.Normalize(code);

            if (normalized is not null)
            {
                model.Report = await reports.GetByTrackingCodeAsync(normalized);
                model.NotFound = model.Report is null;
            }
            else if (!string.IsNullOrWhiteSpace(code))
            {
                model.NotFound = true;
            }

            return View(model);
        }

        // English / Filipino switch in the header. Remembered in a cookie for a year.
        [HttpGet]
        public IActionResult Language(string lang, string? returnUrl)
        {
            var uiCulture = lang == "fil" ? "fil" : "en";
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("en", uiCulture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });

            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Create));
        }

        private const string SubmittedKey = "SubmittedReportId";

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
