using Microsoft.AspNetCore.Mvc;

namespace MarikinAlert.Web.Controllers
{
    public class ReportsController : Controller
    {
        // 1. The Emergency Form (Public Page)
        public IActionResult Create()
        {
            return View();
        }

        // 2. The Live Feed (Admin Page)
        public IActionResult Dashboard()
        {
            return View();
        }

        // 3. The History Page
        public IActionResult Archive()
        {
            return View();
        }
    }
}