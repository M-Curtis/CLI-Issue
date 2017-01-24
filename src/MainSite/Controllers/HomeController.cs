#region Usings

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

#endregion

namespace MainSite.Controllers
{
    [RequireHttps]
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        public IActionResult NotAuthorized()
        {
            return View();
        }
    }
}