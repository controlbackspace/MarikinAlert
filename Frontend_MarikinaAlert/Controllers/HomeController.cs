using System.Diagnostics;
using Frontend_MarikinaAlert.Models;
using Microsoft.AspNetCore.Mvc;

namespace Frontend_MarikinaAlert.Controllers
{
    public class HomeController : Controller
    {
        // Target of UseExceptionHandler and UseStatusCodePagesWithReExecute in Program.cs.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? code) => View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code
        });
    }
}
