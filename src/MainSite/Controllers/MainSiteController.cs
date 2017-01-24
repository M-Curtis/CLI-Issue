using Microsoft.AspNetCore.Mvc;

namespace MainSite.Controllers
{
    public class MainSiteController : Controller
    {
        public IActionResult Index(string returnurl)
        {
            ViewBag.link = returnurl;
            return View();
        }
    }
}
