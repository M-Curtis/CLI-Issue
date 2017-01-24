#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MainSite.Data;
using MainSite.Debug;
using MainSite.Models;
using MainSite.Models.CompaniesViewModels;
using MainSite.Models.MachineViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;
using NuGet.Protocol.Core.v3;

#endregion
namespace MainSite.Controllers
{
    [Authorize]
    public class MachineController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public MachineController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index(int? id)
        {
            if (id == null)
                return Globals.Error(404);
            if (_context.Machines.Any(m => m.Id == id) == false)
                return Globals.Error(404);
            var z = _context.Companies.Join(_context.Machines, c => c.Id, m => m.Cid, (c, m) => new {c, m})
                .Where(t => t.m.Id == id)
                .Select(t => t.c).First();
            ViewBag.CID = z.Id;
            ViewBag.CName = z.CompanyName;
            ViewBag.Products = GetMachineProducts(id.Value);

            var model = new MachineMachineCredentialsViewModel
            {
                Machine = _context.Machines.First(m => m.Id == id),
                Credentials = GetMachineCredentials(id.Value).Result
            };

            return View(model);
        }



        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return Globals.Error(400);
            if (_context.Machines.Any(m => m.Id == id) == false)
                return Globals.Error(404);

            var delMachine = _context.Machines.First(m => m.Id == id);

            bool any;
            try
            {
                any = _context.ProductLinks.Any(l => l.Mid == delMachine.Id);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"User:({_userManager.GetUserName(User)}) Exception: {e}");
                return Globals.Error(400);
            }
            if (any)
            {
                TempData["notify"] = Globals.Notification(Notification.Error,
                    "<strong>Error!</strong> Products still assigned to the Machine, remove all products from machine or transfer them to new machines in order to delete.");
                return RedirectToAction("Index", new { id = delMachine.Id });
            }
            _context.Remove(delMachine);
            await _context.SaveChangesAsync();
            TempData["notify"] = Globals.Notification(Notification.Success,
                $"Successfully deleted {delMachine.Type} - {delMachine.ModelNumber} from " +
                $"{_context.Companies.First(c => c.Id == delMachine.Cid).CompanyName}");
            return RedirectToAction("Details", "Companies", new { id = delMachine.Cid });
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return Globals.Error(401);
            if (!MachineExists(id.Value).Result)
                return Globals.Error(404);

            var model = await _context.Machines.FirstAsync(m => m.Id == id);
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Edit()
        {
            var inMid = HttpContext.Request.Form["mid"];
            int mid;
            if (!int.TryParse(inMid, out mid))
                return Globals.Error(404);

            var machine = await _context.Machines.FirstAsync(m => m.Id == mid);
            machine.HostName = HttpContext.Request.Form["hName"];

            try
            {
                _context.Update(machine);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success, "Successfully updated hostname.");
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error, "Unable to save changes to database, please try again.");
                return RedirectToAction("Index", new {id = mid});
            }
            
            return RedirectToAction("Index", new {id = mid});
        }


        public IActionResult AddProduct(int? id)
        {
            if (id == null)
                return Globals.Error(401);
            if (_context.Machines.Any(ma => ma.Id == id) == false)
                return Globals.Error(404);

            var x = _context.Machines.First(m => m.Id == id);
            ViewBag.Title = $"Add Product to {x.Type} - {x.ModelNumber}";

            var productmodel = _context.Products.ToList()
                                .OrderBy(p => p.ProductName)
                                .Select(product => new ProductViewModel {
                                            Text = product.ProductName,
                                            Value = product.Id.ToString()
                                      }).ToList();
            productmodel.Add(new ProductViewModel { Text = "Other", Value = "__" });

            ViewBag.cid = (from m in _context.Machines where m.Id == id select m).First().Cid;
            ViewBag.mid = id;
            return View(productmodel);
        }
        [HttpPost]
        public async Task<IActionResult> AddProduct()
        {
            var machine = Convert.ToInt32(HttpContext.Request.Form["mid"]);
            var compId = Convert.ToInt32(HttpContext.Request.Form["cid"]);

            var link = new ProductLink
            {
                Cid = compId,
                StartDate = DateTime.ParseExact(HttpContext.Request.Form["daterange"].ToString().Split('-')[0].Trim(),
                    "dd/MM/yyyy", null),
                ExpiryDate = DateTime.ParseExact(HttpContext.Request.Form["daterange"].ToString().Split('-')[1].Trim(),
                    "dd/MM/yyyy", null),
                Mid = machine
            };

            var version = HttpContext.Request.Form["productVersion"];
            if (!string.IsNullOrWhiteSpace(version))
                link.Version = version;
            var lKey = HttpContext.Request.Form["lKey"];
            if (!string.IsNullOrWhiteSpace(lKey))
                link.LicenseKey = lKey;

            if (HttpContext.Request.Form["options"] == "__")
            {
                link.Other = HttpContext.Request.Form["otherProductName"];
            }
            else
            {
                var pid = Convert.ToInt32(HttpContext.Request.Form["options"]);
                foreach (var product in GetCompanyProducts(compId))
                {
                    if (product.Id == pid)
                    {
                        var lUpdate = (from c in _context.Companies
                            where c.Id == compId
                            join l in _context.ProductLinks on c.Id equals l.Cid
                            where l.Pid == pid
                            select l).FirstOrDefault();
                        if (lUpdate.Mid == null)
                        {
                            lUpdate.Mid = machine;
                            lUpdate.ExpiryDate = link.ExpiryDate;
                            lUpdate.StartDate = link.StartDate;
                            lUpdate.Version = link.Version;
                            try
                            {
                                _context.Update(lUpdate);
                                await _context.SaveChangesAsync();
                                TempData["notify"] = Globals.Notification(Notification.Info,
                                $"{(from c in _context.Companies where c.Id == compId select c).First().CompanyName} - " +
                                $"{(from m in _context.Machines where m.Id == machine select m).First().ModelNumber} - (" +
                                $"{link.Version}) , ({link.StartDate} - {link.ExpiryDate})");
                            }
                            catch (Exception e)
                            {
                                System.Diagnostics.Debug.WriteLine(e.ToString());
                                TempData["notify"] = Globals.Notification(Notification.Error,
                                    $"<strong>Failed</strong> adding product, ({lUpdate.Other}) to " +
                                    $"{_context.Companies.FirstOrDefault(c => c.Id == compId).CompanyName}" +
                                    $" - {_context.Machines.FirstOrDefault(m => m.Id == machine).Type}" +
                                    $" {_context.Machines.FirstOrDefault(m => m.Id == machine).ModelNumber}.");
                            }
                            return RedirectToAction("Index", new {id = machine});
                        }
                    }
                }
                link.Pid = pid;
            }

            try
            {
                _context.Add(link);
                await _context.SaveChangesAsync();
            }
            catch(Exception e)
            {
                Globals.SendError(e);
                TempData["notify"] = Globals.Notification(Notification.Error, $"Failed</strong> adding product, ({HttpContext.Request.Form["options"]}) to " +
                    $"{_context.Companies.FirstOrDefault(c => c.Id == compId).CompanyName}" +
                    $" {_context.Machines.FirstOrDefault(m => m.Id == machine).Type}" +
                    $" {_context.Machines.FirstOrDefault(m => m.Id == machine).ModelNumber}.");
            }

            return RedirectToAction("Index", new {id = machine});
        }


        public async Task<IActionResult> DeleteProduct(int id, int mid)
        {
            ProductLink link;
            try
            {
                link = _context.ProductLinks.First(l => l.Id == id);
            }
            catch (Exception e)
            {
                Globals.SendError(e);
                TempData["notify"] = Globals.Notification(Notification.Error, $"Error! Unable to find product with id: {id}, in the database, somebody may have deleted the item before you or you made a bad request.");
                return RedirectToAction("Index", new {id});
            }
            try
            {
                _context.Remove(link);
                await _context.SaveChangesAsync();
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error, "Unable to remove product and save changes on the database.");
                return RedirectToAction("Index", new {id});
            }
            var name = link.Other ?? _context.Products.First(p => p.Id == link.Pid).ProductName;
            TempData["notify"] = Globals.Notification(Notification.Success, $"Successfully removed {name}");
            return RedirectToAction("Index", new {id = mid});
        }
        

        public IActionResult MoveProduct(int id)
        {
            var cid = _context.ProductLinks.Where(l => l.Id == id).Select(l => l.Cid).First();
            var model = _context.Machines.Where(m => m.Cid == cid).ToList().Select(machine => new MachineSelectionViewModel
            {
                Id = machine.Id,
                Name = $"{machine.Type} - {machine.ModelNumber}"
            }).ToList();

            var fromMachine = _context.ProductLinks.Where(l => l.Id == id)
                .Join(_context.Machines, l => l.Mid, m => m.Id, (l, m) => m).First();
            ViewBag.mid = fromMachine.Id;
            ViewBag.mName = $"{fromMachine.Type} - {fromMachine.ModelNumber}";
            ViewBag.lid = id;
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> MoveProduct()
        {
            var id = Convert.ToInt32(HttpContext.Request.Form["lid"]);
            var s1 = _context.Machines.First(m => m.Id == _context.ProductLinks.First(l => l.Id == id).Mid);
            var link = _context.ProductLinks.First(l => l.Id == id);
            int output;
            var remove = false;
            if (HttpContext.Request.Form["mSel"] == "__")
            {
                link.Mid = null;
                remove = true;
            }
            else if (int.TryParse(HttpContext.Request.Form["mSel"], out output))
                link.Mid = output;
            else
            {
                TempData["notify"] = Globals.Notification(Notification.Info, "Form content got corrupted or something along the way.. probably.");
                return Globals.Error(400);
            }
            try
            {
                _context.Update(link);
                await _context.SaveChangesAsync();
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error, "Unable to move product and save changes on the database.");
                return RedirectToAction("Index", link.Mid);
            }
            var s1Name = $"{s1.Type} - {s1.ModelNumber}";
            var s2 = _context.Machines.First(m => m.Id == link.Mid);
            var s2Name = !remove ? $"{s2.Type} - {s2.ModelNumber}" : "Nothing";
            TempData["notify"] = Globals.Notification(Notification.Success, $"Successfully moved product from {s1Name} to {s2Name}.");
            return remove == false ? RedirectToAction("Index", new { MID = link.Mid }) : RedirectToAction("Index", "Companies", new {id = link.Cid});
        }


        public IActionResult EditProduct(int? id)
        {
            if (id == null)
            {
                return Globals.Error(400);
            }
            if (_context.ProductLinks.Any(l => l.Id == id) == false)
            {
                return Globals.Error(404);
            }
            var model = _context.ProductLinks.First(l => l.Id == id);
            ViewBag.Date = $"{model.StartDate} - {model.ExpiryDate}";
            ViewBag.Name = model.Other ??_context.Products.First(p => p.Id == model.Pid).ProductName;
            return View(model);
        }

        private class AnonymousMidClass
        {
            public AnonymousMidClass(int? id)
            {
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct()
        {
            int id;
            var parse = int.TryParse(HttpContext.Request.Form["lid"], out id);
            if (parse == false)
            {
                return Globals.Error(400);
            }
            if (_context.ProductLinks.Any(l => l.Id == id) == false)
            {
                return Globals.Error(404);
            }
            var link = _context.ProductLinks.First(l => l.Id == id);
            var split = HttpContext.Request.Form["daterange"].ToString().Split('-');
            link.StartDate = DateTime.ParseExact(split[0].Trim(), "dd/MM/yyyy", null);
            link.ExpiryDate = DateTime.ParseExact(split[1].Trim(), "dd/MM/yyyy", null);
            link.LicenseKey = HttpContext.Request.Form["lKey"];
            link.Version = HttpContext.Request.Form["Version"];
            try
            {
                _context.Update(link);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success, "Successfully updated product definition in the database");
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error, "Error updating product please try again.");
                return RedirectToAction("Index", new AnonymousMidClass(link.Mid));
            }
            return RedirectToAction("Index", new AnonymousMidClass(link.Mid));
        }


        public IActionResult AddCredentials(int? id)
        {
            if (id == null || id <= 0)
            {
                return Globals.Error(400);
            }
            if (!_context.Machines.Any(m => m.Id == id))
            {
                return Globals.Error(404);
            }
            ViewBag.mid = id;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddCredentials()
        {
            var midIn = HttpContext.Request.Form["mid"];
            int mid;
            if (!int.TryParse(midIn, out mid))
                return Globals.Error(400);

            var credentials = new MachineCredentials
            {
                Mid = mid,
                Username = HttpContext.Request.Form["usr"],
                Password = HttpContext.Request.Form["pwd"]
            };
            try
            {
                _context.Add(credentials);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success, "Successfully added credentials to database.");
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error, "Error adding credentials to database, please try again.");
            }
            return RedirectToAction("Index", new {id = mid});
        }


        public IActionResult DeleteCredentials(int? cid)
        {
            if (cid == null || cid <= 0)
                return Globals.Error(400);
            if (!_context.MachineCredentials.Any(i => i.Id == cid))
                return Globals.Error(404);


            var creds = _context.MachineCredentials.First(i => i.Id == cid.Value);
            ViewBag.mId = _context.Machines.First(m => m.Id == creds.Mid).Id;
            ViewBag.cid = cid;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> DeleteCredentials()
        {
            var cidIn = HttpContext.Request.Form["cid"];
            int cid;
            if (!int.TryParse(cidIn, out cid))
                return Globals.Error(400);

            if (!_context.MachineCredentials.Any(i => i.Id == cid))
                return Globals.Error(404);

            var mCreds = _context.MachineCredentials.First(i => i.Id == cid);

            try
            {
                _context.Remove(mCreds);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success, "Successfully removed credentials.");
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error, "Error removing credentials.");
            }

            var mid = _context.Machines.First(m => m.Id == mCreds.Mid).Id;
            return RedirectToAction("Index", new {id = mid});
        }


        #region Methods
        private List<MachineProductsViewModel> GetMachineProducts(int id) => _context.Machines
            .Where(m => m.Id == id)
            .Join(_context.ProductLinks, m => m.Id, l => l.Mid.Value, (m, l) => l)
            .ToList()
            .Select(l => new MachineProductsViewModel
            {
                Id = l.Id,
                Expires = l.ExpiryDate,
                LicenseKey = l.LicenseKey,
                Version = l.Version,
                ProductName = l.Pid == null ? l.Other : (from p in _context.Products where p.Id == l.Pid.Value select p).First().ProductName
            })
            .ToList();

        private IEnumerable<Product> GetCompanyProducts(int cid) => _context.Companies.Join(_context.ProductLinks, c => c.Id, j => j.Cid, (c, j) => new {c, j})
            .Join(_context.Products, t => t.j.Pid, p => p.Id, (t, p) => new {t, p})
            .Where(t => t.t.c.Id == cid)
            .Select(t => t.p).ToList();

        private async Task<bool> MachineExists(int mid)
        {
            var mList = await _context.Machines.ToListAsync();
            return mList.Any(m => m.Id == mid);
        }

        private async Task<List<MachineCredentials>> GetMachineCredentials(int mid)
        {
            var credentialsList = await _context.MachineCredentials.ToListAsync();
            return credentialsList.Where(credentialSet => credentialSet.Mid == mid).ToList();
        }
        #endregion

    }
}