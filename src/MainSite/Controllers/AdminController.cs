#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MainSite.Data;
using MainSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

#endregion

namespace MainSite.Controllers
{
    [RequireHttps]
    [Authorize("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Claim()
        {
            var claims = _context.UserClaims.ToList();
            ViewBag.claims = claims;
            ViewBag.users = await GetUsers();
            return View("../Admin/Claim/Index");
        }

        public IActionResult Claim_Edit()
        {
            return View("../Admin/Claim/Edit");
        }

        public IActionResult Claim_Details(int id)
        {
            var claims = _context.UserClaims.ToList();
            ViewBag.claims = claims;
            ViewBag.id = id;
            return View("../Admin/Claim/Details");
        }

        public IActionResult Claim_Delete(int? id)
        {
            if (id != null)
            {
                return View("../Admin/Claim/Delete");
            }
            TempData["notify"] = Globals.Notification(Notification.Error, "Bad or no ID.");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Claim_Delete(int id)
        {
            if (id <= 0)
                return NotFound();

            var claims = _context.UserClaims.ToList();
            if (claims == null) throw new ArgumentNullException(nameof(claims));
            var claim = (from c in claims where c.Id == id select c).First();
            
            var task = await _userManager.RemoveClaimAsync(await _userManager.FindByIdAsync(claim.UserId), new Claim(claim.ClaimType, claim.ClaimValue));

            if (!task.Succeeded)
            {
                TempData["notify"] = Globals.Notification(Notification.Error, 
                    $"<strong>Failed!</strong> {claim.ClaimType} ({claim.ClaimValue}) was not removed from {_userManager.FindByIdAsync(claim.UserId).Result.NormalizedUserName}.");
                System.Diagnostics.Debug.WriteLine($"FAILED:  RemoveClaimAsync Task failed on user ({_userManager.FindByIdAsync(claim.UserId).Result.UserName}).");
            }
            else
            {
                TempData["notify"] = Globals.Notification(Notification.Success, 
                    $"<strong>Success!</strong> {claim.ClaimType} ({claim.ClaimValue}) removed from {_userManager.FindByIdAsync(claim.UserId).Result.NormalizedUserName}.");
            }
            
            return RedirectToAction("Claim");
        }

        public IActionResult Claim_Add(int? uid)
        {
            return View("../Admin/Claim/AddClaim");
        }
        [HttpPost]
        public IActionResult Claim_Add()
        {
            TempData["notify"] = Globals.Notification(Notification.Success, "<strong>Success!</strong>");
            return RedirectToAction("Index");
        }

        //Methods
        private async Task<List<string>> GetUsers()
        {
            var allclaims = _context.UserClaims.ToList();
            var claimusers = new List<string>();
            foreach (var claim in allclaims)
            {
                var user = await _userManager.FindByIdAsync(claim.UserId);
                claimusers.Add(user.UserName);
            }
            return claimusers;
        }

        public async Task<IActionResult> AssignGroup(string user, string claim)
        {
            var edituser = await _userManager.FindByIdAsync(user);
            await _userManager.AddClaimAsync(edituser, new Claim(claim, "1"));
            return RedirectToAction("Claim", "Admin");
        }


    }
}