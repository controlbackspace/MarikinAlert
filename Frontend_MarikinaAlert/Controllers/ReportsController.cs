using Microsoft.AspNetCore.Mvc;
using Frontend_MarikinaAlert.Services;
using Frontend_MarikinaAlert.Models;
using System.Threading.Tasks;

namespace Frontend_MarikinaAlert.Controllers
{
    public class ReportsController : Controller
    {
        private readonly IDisasterTriageService _triageService;

        public ReportsController(IDisasterTriageService triageService)
        {
            _triageService = triageService;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // CHANGE: Added 'string contactNumber' to match the Form Input Name
        public async Task<IActionResult> Create(string rawMessage, string senderName, string contactNumber, string location)
        {
            if (!string.IsNullOrEmpty(rawMessage))
            {
                // CHANGE: Pass 'contactNumber' to the service
                await _triageService.TriageAndAnalyzeAsync(rawMessage, senderName, contactNumber, location);
                return RedirectToAction("Dashboard");
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var reports = await _triageService.GetAllReportsAsync();
            return View(reports);
        }
    }
}