#region Usings

using System.IO;
using MainSite.Data;
using Microsoft.AspNetCore.Mvc;

#endregion

namespace MainSite.Controllers
{
    public class ErrorController : Controller
    {
        // GET: /<controller>/
        public IActionResult Index(int id)
        {
            if (id == 400 || id == 401 || id == 403 || id == 404 || id == 500)
            {
                if(TempData["notify"] == null || !string.IsNullOrWhiteSpace(TempData["notify"].ToString()))
                return View($"../Error/{id}");
            }
                
            return RedirectToAction("Index", new {id = 404});
        }

        private bool ErrorPageExists(int id)
        {
            return System.IO.File.Exists(Path.Combine(Globals.BasePath, "\\Views\\Error\\",
                    Path.Combine(id.ToString(), ".cshtml")));
        }
    }
}
