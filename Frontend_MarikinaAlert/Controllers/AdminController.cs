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
    public class AdminController(IReportRepository reports, IAgencyRepository agencies, ITeamRepository teams,
        IDisasterTriageService triage, IConfiguration config) : Controller
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
        public async Task<IActionResult> Dashboard(ReportCategory? category, ReportStatus? status, string? barangay) =>
            View(await BuildFeedAsync(category, status, barangay));

        // Polled by admin.js every few seconds; returns only the incident list.
        [HttpGet]
        public async Task<IActionResult> Feed(ReportCategory? category, ReportStatus? status, string? barangay) =>
            PartialView("_IncidentFeed", await BuildFeedAsync(category, status, barangay));

        private async Task<IncidentFeedViewModel> BuildFeedAsync(ReportCategory? category, ReportStatus? status, string? barangay)
        {
            // Load every open report once: duplicates, counts and the barangay summary must consider all of them, even when filtered.
            var open = await reports.GetOpenAsync();
            var now = DateTime.UtcNow;
            var overdue = open.Where(r => IncidentInsights.IsOverdue(r, now)).Select(r => r.Id).ToHashSet();
            barangay = string.IsNullOrWhiteSpace(barangay) ? null : barangay;
            var contacts = await reports.GetContactsAsync(open.Select(r => r.Id).ToList());
            var directory = (await agencies.GetAllAsync()).Where(a => a.IsActive)
                .ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);

            return new IncidentFeedViewModel
            {
                Reports = open
                    .Where(r => (category is null || r.Category == category)
                             && (status is null || r.Status == status)
                             && (barangay is null || r.Barangay == barangay))
                    .ToList(),
                Category = category,
                Status = status,
                Barangay = barangay,
                CriticalCount = open.Count(r => r.Priority == ReportPriority.Critical),
                OpenCount = open.Count,
                OverdueIds = overdue,
                DuplicateOf = IncidentInsights.FindDuplicates(open),
                BarangaySummary = open
                    .GroupBy(r => r.Barangay)
                    .Select(g => new BarangayCount(g.Key, g.Count(), g.Count(r => r.Priority == ReportPriority.Critical), g.Count(r => overdue.Contains(r.Id))))
                    .OrderByDescending(b => b.Critical).ThenByDescending(b => b.Open).ThenBy(b => b.Barangay)
                    .ToList(),
                Agencies = directory,
                Contacts = contacts.ToLookup(c => c.ReportId),
                Teams = await teams.GetAllAsync()
            };
        }

        // ---------- New report (calls, radio, walk-ins) ----------

        [HttpGet]
        public IActionResult NewReport() => View(new LogReportViewModel { Source = ReportSource.PhoneCall });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NewReport(LogReportViewModel model)
        {
            model.Barangay = model.Barangay?.Normalize(System.Text.NormalizationForm.FormC);
            if (model.Barangay is not null && !Marikina.Barangays.Contains(model.Barangay))
                ModelState.AddModelError(nameof(model.Barangay), "Choose a barangay from the list.");
            if (model.Source == ReportSource.App)
                ModelState.AddModelError(nameof(model.Source), "Choose how this report came in.");

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
                Source = model.Source!.Value,
                LoggedBy = User.Identity?.Name,
                TrackingCode = await TrackingCodes.NewAsync(reports)
            });

            // Shown once on the dashboard so the dispatcher can read the tracking code to the caller.
            TempData["LoggedRef"] = $"MA-{report.Id:D5}";
            TempData["LoggedCode"] = report.TrackingCodeDisplay;
            return RedirectToAction(nameof(Dashboard));
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

        // Records one call or radio attempt to an agency. The call itself happens on the dispatcher's phone or radio.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LogContact(int id, string? agency, string? number, ContactMethod method, ContactOutcome outcome)
        {
            // An unknown enum value binds as the default (Phone / Answered) with a ModelState error, so check both.
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(agency) || !Enum.IsDefined(method) || !Enum.IsDefined(outcome))
                return BadRequest();
            if (await reports.GetByIdAsync(id) is null) return NotFound();

            await reports.AddContactAsync(new ContactAttempt
            {
                ReportId = id,
                AgencyName = Truncate(agency.Trim(), 100),
                Number = method == ContactMethod.Phone && Clean(number) is string n ? Truncate(n, 30) : null,
                Method = method,
                Outcome = outcome,
                LoggedBy = User.Identity?.Name
            });

            if (Request.Headers.ContainsKey("X-Requested-With")) return NoContent();
            return RedirectToAction(nameof(Dashboard));
        }

        // Closing a report sends any teams still out on it back to base.
        [HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> Resolve(int id) =>
            ChangeAsync(id, r => r.Status = ReportStatus.Resolved, after: teams.ReleaseFromReportAsync);

        [HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> Dismiss(int id) =>
            ChangeAsync(id, r => r.Status = ReportStatus.Dismissed, after: teams.ReleaseFromReportAsync);

        // ---------- Response teams ----------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTeam(int id, int teamId)
        {
            var report = await reports.GetByIdAsync(id);
            var team = await teams.GetByIdAsync(teamId);
            if (report is null || team is null) return NotFound();
            // Another dispatcher may have sent this team out a moment ago.
            if (team.Status != TeamStatus.Available || !report.IsOpen) return Conflict();

            team.Status = TeamStatus.Dispatched;
            team.AssignedReportId = report.Id;
            team.StatusChangedAtUtc = DateTime.UtcNow;
            await teams.UpdateAsync(team);

            // Sending a team means the report is confirmed and handed to that team's agency.
            report.IsVerified = true;
            report.RelayedTo ??= team.AgencyName;
            if (report.Status is ReportStatus.Active or ReportStatus.Verified) report.Status = ReportStatus.Relayed;
            await reports.UpdateAsync(report);

            return Done();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetTeamStatus(int teamId, TeamStatus status, string? returnUrl)
        {
            if (!ModelState.IsValid || !Enum.IsDefined(status)) return BadRequest();
            var team = await teams.GetByIdAsync(teamId);
            if (team is null) return NotFound();
            if (!CanMove(team.Status, status)) return Conflict();

            team.Status = status;
            team.StatusChangedAtUtc = DateTime.UtcNow;
            if (status is TeamStatus.Available or TeamStatus.OutOfService) team.AssignedReportId = null;
            await teams.UpdateAsync(team);

            return Done(returnUrl);
        }

        // Normal cycle: Available -> Dispatched -> On scene -> Returning -> Available.
        // A dispatched team can be recalled straight to Available; a team can go out of service only while not on a job.
        private static bool CanMove(TeamStatus from, TeamStatus to) => (from, to) switch
        {
            (TeamStatus.Dispatched, TeamStatus.OnScene) => true,
            (TeamStatus.Dispatched or TeamStatus.OnScene, TeamStatus.Returning) => true,
            (TeamStatus.Dispatched or TeamStatus.Returning or TeamStatus.OutOfService, TeamStatus.Available) => true,
            (TeamStatus.Available or TeamStatus.Returning, TeamStatus.OutOfService) => true,
            _ => false
        };

        [HttpGet]
        public async Task<IActionResult> Teams() => View(await BuildTeamsAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTeam(ResponseTeam model)
        {
            model.Name = model.Name?.Trim() ?? "";
            model.AgencyName = model.AgencyName?.Trim() ?? "";
            model.BaseBarangay = model.BaseBarangay?.Normalize(System.Text.NormalizationForm.FormC) ?? "";
            model.CallSign = Clean(model.CallSign);

            var all = await teams.GetAllAsync();
            if (all.Any(t => t.Id != model.Id && string.Equals(t.Name, model.Name, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError(nameof(ResponseTeam.Name), "Another team already has this name.");
            if (model.BaseBarangay.Length > 0 && !Marikina.Barangays.Contains(model.BaseBarangay))
                ModelState.AddModelError(nameof(ResponseTeam.BaseBarangay), "Choose a barangay from the list.");

            if (!ModelState.IsValid)
            {
                var vm = await BuildTeamsAsync();
                vm.Invalid = model;
                return View(nameof(Teams), vm);
            }

            if (model.Id == 0)
            {
                model.Status = TeamStatus.Available;
                model.AssignedReportId = null;
                await teams.AddAsync(model);
            }
            else
            {
                // Status changes go through SetTeamStatus; this form only edits the details.
                var existing = await teams.GetByIdAsync(model.Id);
                if (existing is null) return NotFound();
                existing.Name = model.Name;
                existing.AgencyName = model.AgencyName;
                existing.BaseBarangay = model.BaseBarangay;
                existing.CallSign = model.CallSign;
                await teams.UpdateAsync(existing);
            }

            TempData["SavedTeam"] = model.Name;
            return RedirectToAction(nameof(Teams));
        }

        private async Task<TeamsViewModel> BuildTeamsAsync() => new()
        {
            Teams = await teams.GetAllAsync(),
            AgencyNames = (await agencies.GetAllAsync()).Where(a => a.IsActive).Select(a => a.Name).ToList()
        };

        // admin.js sends X-Requested-With; plain form posts (no JS) get a normal redirect.
        private IActionResult Done(string? returnUrl = null)
        {
            if (Request.Headers.ContainsKey("X-Requested-With")) return NoContent();
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Dashboard));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> Reopen(int id, string? returnUrl) => ChangeAsync(id, r =>
            r.Status = r.RelayedTo is not null ? ReportStatus.Relayed : r.IsVerified ? ReportStatus.Verified : ReportStatus.Active,
            returnUrl);

        private async Task<IActionResult> ChangeAsync(int id, Action<DisasterReport> change, string? returnUrl = null,
            Func<int, Task>? after = null)
        {
            var report = await reports.GetByIdAsync(id);
            if (report is null) return NotFound();

            change(report);
            await reports.UpdateAsync(report);
            if (after is not null) await after(report.Id);

            return Done(returnUrl);
        }

        // ---------- History ----------

        [HttpGet]
        public async Task<IActionResult> History(ReportStatus? status, string? q) => View(new HistoryViewModel
        {
            Reports = await reports.GetClosedAsync(status, q),
            Status = status,
            Search = q
        });

        // ---------- Agency directory ----------

        [HttpGet]
        public async Task<IActionResult> Agencies() =>
            View(new AgenciesViewModel { Agencies = await agencies.GetAllAsync() });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAgency(Agency model)
        {
            model.Name = model.Name?.Trim() ?? "";
            model.Hotline = Clean(model.Hotline);
            model.BackupNumber = Clean(model.BackupNumber);
            model.RadioChannel = Clean(model.RadioChannel);
            model.Notes = Clean(model.Notes);
            model.NumberSource = Clean(model.NumberSource);

            var all = await agencies.GetAllAsync();
            if (all.Any(a => a.Id != model.Id && string.Equals(a.Name, model.Name, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError(nameof(Agency.Name), "Another agency already has this name.");
            if (model.Hotline is null && model.BackupNumber is not null)
                ModelState.AddModelError(nameof(Agency.Hotline), "Enter the main hotline before a backup number.");

            if (!ModelState.IsValid)
                return View(nameof(Agencies), new AgenciesViewModel { Agencies = all, Invalid = model });

            if (model.Id == 0)
            {
                await agencies.AddAsync(model);
            }
            else
            {
                var existing = await agencies.GetByIdAsync(model.Id);
                if (existing is null) return NotFound();
                existing.Name = model.Name;
                existing.Hotline = model.Hotline;
                existing.BackupNumber = model.BackupNumber;
                existing.RadioChannel = model.RadioChannel;
                existing.Notes = model.Notes;
                existing.NumberSource = model.NumberSource;
                existing.IsActive = model.IsActive;
                await agencies.UpdateAsync(existing);
            }

            TempData["SavedAgency"] = model.Name;
            return RedirectToAction(nameof(Agencies));
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

        private static bool FixedTimeEquals(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(a)),
                SHA256.HashData(Encoding.UTF8.GetBytes(b)));
    }
}
