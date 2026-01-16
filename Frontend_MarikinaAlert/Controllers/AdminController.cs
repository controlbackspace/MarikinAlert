using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Needed for Session
using Frontend_MarikinaAlert.Services;
using Frontend_MarikinaAlert.Models; // Ensure this matches your namespace

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
        // 1. THE LOGIN PAGE (Public)
        // ==========================================
        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, go straight to dashboard
            if (HttpContext.Session.GetString("UserRole") == "Admin")
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // HARDCODED SECURITY (Simple for Student Projects)
            // You can change "rescue123" to whatever password you want
            if (username == "admin" && password == "rescue123")
            {
                // MARK THE USER AS LOGGED IN
                HttpContext.Session.SetString("UserRole", "Admin");
                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Access Denied: Invalid Credentials";
            return View();
        }
        [HttpPost]
        public IActionResult UpdateStatus(Guid id, ReportStatus status)
        {
            // NOTE: In a real app, you would call _triageService.UpdateStatus(id, status).
            // For this prototype, we are just refreshing the page since we haven't wired 
            // the Update logic to the database yet.

            // TODO: Ask Student 1 to add "UpdateStatus" to the Repository.

            return RedirectToAction("Dashboard");
        }

        // ==========================================
        // 2. THE DASHBOARD (Protected)
        // ==========================================
        public async Task<IActionResult> Dashboard()
        {
            // SECURITY CHECK: STOP! Are you an admin?
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                // If not, kick them back to login
                return RedirectToAction("Login");
            }

            // If yes, load the data
            try
            {
                var reports = await _triageService.GetAllReportsAsync();
                return View(reports);
            }
            catch
            {
                return View(new List<DisasterReport>());
            }
        }

        // ==========================================
        // 3. LOGOUT
        // ==========================================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Delete the memory
            return RedirectToAction("Login");
        }
    }
}