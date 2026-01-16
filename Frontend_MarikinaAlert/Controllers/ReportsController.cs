using Frontend_MarikinaAlert.Models;
using Frontend_MarikinaAlert.Services;
using MarikinAlert.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
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

        // ==========================================
        // 1. THE PUBLIC FEED (Default Page)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Everyone can see this. No login required.
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
        // 2. THE REPORT FORM
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new SubmitReportViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(SubmitReportViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _triageService.TriageAndAnalyzeAsync(
                model.RawMessage,
                model.SenderName,
                model.Location,
                model.ContactNumber
            );

            // FIX: Redirect back to the Public Feed, NOT the Admin Login
            return RedirectToAction("Index");
        }

        public IActionResult Archive()
        {
            return View();
        }
    }
}