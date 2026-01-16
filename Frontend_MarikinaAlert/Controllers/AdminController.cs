using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Needed for Session
using Frontend_MarikinaAlert.Services;
using Frontend_MarikinaAlert.Models;
using System.Linq; // <--- CRITICAL: Needed for filtering (Where)
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Frontend_MarikinaAlert.Controllers
{
    public class AdminController : Controller
    {
        private readonly IDisasterTriageService _triageService;

        public AdminController(IDisasterTriageService triageService)
        {
            _triageService = triageService;
        }

        // ==========================================
        // 1. AUTHENTICATION (Login/Logout)
        // ==========================================
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserRole") == "Admin")
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Use helper method for cleaner logic
            if (IsValidUser(username, password))
            {
                HttpContext.Session.SetString("UserRole", "Admin");
                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Access Denied: Invalid Credentials";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ==========================================
        // 2. THE DASHBOARD (Active Incidents Only)
        // ==========================================
        public async Task<IActionResult> Dashboard()
        {
            // Security Check
            if (HttpContext.Session.GetString("UserRole") != "Admin") return RedirectToAction("Login");

            try
            {
                var reports = await _triageService.GetAllReportsAsync();

                // FILTER: Only show Active or OnGoing (Hide Resolved)
                var activeReports = reports.Where(r => r.Status != ReportStatus.Resolved).ToList();

                return View(activeReports);
            }
            catch
            {
                return View(new List<DisasterReport>());
            }
        }

        // ==========================================
        // 3. THE HISTORY (Resolved Incidents Only)
        // ==========================================
        public async Task<IActionResult> History()
        {
            // Security Check
            if (HttpContext.Session.GetString("UserRole") != "Admin") return RedirectToAction("Login");

            try
            {
                var reports = await _triageService.GetAllReportsAsync();

                // FILTER: Only show Resolved
                var historyReports = reports.Where(r => r.Status == ReportStatus.Resolved).ToList();

                return View(historyReports);
            }
            catch
            {
                return View(new List<DisasterReport>());
            }
        }

        // ==========================================
        // 4. ACTIONS (Update Status/Category)
        // ==========================================
        [HttpPost]
        public IActionResult UpdateStatus(Guid id, ReportStatus status)
        {
            // TODO: [BACKEND TEAM] Connect this to _triageService.UpdateStatus(id, status)
            // For prototype: We just reload the page. Since we don't have a real database 
            // connected to this specific action yet, the change might not persist 
            // if you restart the app, but the UI interaction works.

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public IActionResult UpdateCategory(Guid id, ReportCategory category)
        {
            // TODO: [BACKEND TEAM] Connect this to _triageService.UpdateCategory(id, category)

            return RedirectToAction("Dashboard");
        }

        // ==========================================
        // 5. HELPER METHODS
        // ==========================================
        private bool IsValidUser(string username, string password)
        {
            // Mock Credentials - easy to replace with Database lookup later
            return username == "admin" && password == "rescue123";
        }
    }
}