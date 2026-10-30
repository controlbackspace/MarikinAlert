using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Frontend_MarikinaAlert.Models;
using Frontend_MarikinaAlert.Repositories;
using Frontend_MarikinaAlert.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frontend_MarikinaAlert.Controllers
{
    // Dispatcher / Incident Triage Officer pages.
    [Authorize]
    public class AdminController(IReportRepository reports, IConfiguration config) : Controller
    {
        // ---------- Login ----------

        [AllowAnonymous, HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Dashboard));
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var expectedUser = config["AdminAccount:Username"] ?? "";
            var expectedPass = config["AdminAccount:Password"] ?? "";

            // Check both even if the username is wrong, so timing doesn't reveal which one failed.
            var userOk = FixedTimeEquals(model.Username!, expectedUser);
            var passOk = FixedTimeEquals(model.Password!, expectedPass);

            if (!userOk || !passOk || expectedPass.Length == 0)
            {
                ModelState.AddModelError("", "Incorrect username or password.");
                model.Password = null;
                return View(model);
            }

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, config["AdminAccount:DisplayName"] ?? model.Username!)],
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return Url.IsLocalUrl(model.ReturnUrl) ? Redirect(model.ReturnUrl!) : RedirectToAction(nameof(Dashboard));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // ---------- Live dashboard ----------

        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        [HttpGet]
        public async Task<IActionResult> Dashboard(ReportCategory? category, ReportStatus? status) =>
            View(await BuildFeedAsync(category, status));

        // Polled by admin.js every few seconds; returns only the incident list.
        [HttpGet]
        public async Task<IActionResult> Feed(ReportCategory? category, ReportStatus? status) =>
            PartialView("_IncidentFeed", await BuildFeedAsync(category, status));

        private async Task<IncidentFeedViewModel> BuildFeedAsync(ReportCategory? category, ReportStatus? status)
        {
            // Load every open report once: duplicates and counts must consider all of them, even when filtered.
            var open = await reports.GetOpenAsync(null, null);
            var now = DateTime.UtcNow;

            return new IncidentFeedViewModel
            {
                Reports = open
                    .Where(r => (category is null || r.Category == category) && (status is null || r.Status == status))
                    .ToList(),
                Category = category,
                Status = status,
                CriticalCount = open.Count(r => r.Priority == ReportPriority.Critical),
                OpenCount = open.Count,
                OverdueIds = open.Where(r => IncidentInsights.IsOverdue(r, now)).Select(r => r.Id).ToHashSet(),
                DuplicateOf = IncidentInsights.FindDuplicates(open)
            };
        }

        // ---------- Actions ----------

        [HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> Verify(int id) => ChangeAsync(id, r =>
        {
            r.IsVerified = true;
            if (r.Status == ReportStatus.Active) r.Status = ReportStatus.Verified;
        });

        [HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> Relay(int id, string agency) => ChangeAsync(id, r =>
        {
            r.RelayedTo = string.IsNullOrWhiteSpace(agency) ? Marikina.SuggestedAgency(r.Category) : agency.Trim();
            r.IsVerified = true; // relaying implies the dispatcher confirmed it
            r.Status = ReportStatus.Relayed;
        });

        [HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> Resolve(int id) => ChangeAsync(id, r => r.Status = ReportStatus.Resolved);

        [HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> Dismiss(int id) => ChangeAsync(id, r => r.Status = ReportStatus.Dismissed);

        [HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> Reopen(int id, string? returnUrl) => ChangeAsync(id, r =>
            r.Status = r.RelayedTo is not null ? ReportStatus.Relayed : r.IsVerified ? ReportStatus.Verified : ReportStatus.Active,
            returnUrl);

        private async Task<IActionResult> ChangeAsync(int id, Action<DisasterReport> change, string? returnUrl = null)
        {
            var report = await reports.GetByIdAsync(id);
            if (report is null) return NotFound();

            change(report);
            await reports.UpdateAsync(report);

            // admin.js sends this header; plain form posts (no JS) get a normal redirect.
            if (Request.Headers.ContainsKey("X-Requested-With")) return NoContent();

            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Dashboard));
        }

        // ---------- History ----------

        [HttpGet]
        public async Task<IActionResult> History(ReportStatus? status, string? q) => View(new HistoryViewModel
        {
            Reports = await reports.GetClosedAsync(status, q),
            Status = status,
            Search = q
        });

        private static bool FixedTimeEquals(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(a)),
                SHA256.HashData(Encoding.UTF8.GetBytes(b)));
    }
}
