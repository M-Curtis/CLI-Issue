#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MainSite.Data;
using MainSite.Models;
using MainSite.Models.CompaniesViewModels;
using MainSite.Models.MachineViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

#endregion

namespace MainSite.Controllers
{
    [RequireHttps]
    public class CompaniesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CompaniesController(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var companyproductmodel = new CompaniesProductsViewModel
            {
                ModelCompany = await GetCompList(),
                ModelProduct = await GetProdList()
            };
            ViewBag.comCount = companyproductmodel.ModelCompany.Count;
            return View(companyproductmodel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProductViewModel model)
        {
            var companyproductmodel = new CompaniesProductsViewModel
            {
                ModelCompany = await GetCompList(),
                ModelProduct = await GetProdList()
            };

            if (ModelState.IsValid)
            {
                var selForm = HttpContext.Request.Form["options"];
                var searchForm = HttpContext.Request.Form["searchinput"];
                var expiredFilter = HttpContext.Request.Form["exChk"].Equals("on");
                if (selForm != "*" || !string.IsNullOrWhiteSpace(searchForm) || expiredFilter)
                {
                    int id;
                    if (int.TryParse(selForm, out id) && string.IsNullOrWhiteSpace(searchForm) == false)
                        companyproductmodel.ModelCompany = SearchQuery(await ProductIdQuery(id), searchForm);

                    else if (!string.IsNullOrWhiteSpace(searchForm))
                        companyproductmodel.ModelCompany = SearchQuery(companyproductmodel.ModelCompany, searchForm);

                    else if (selForm != "*" && int.TryParse(selForm, out id))
                        companyproductmodel.ModelCompany = await ProductIdQuery(id);

                    if (expiredFilter)
                        companyproductmodel.ModelCompany = await ExpiredQuery(companyproductmodel.ModelCompany);


                    //Set ViewData
                    ViewBag.compList = companyproductmodel.ModelCompany.OrderBy(c => c.Name);
                    ViewBag.comCount = companyproductmodel.ModelCompany.Count.ToString();
                    return View(companyproductmodel);
                }
                return RedirectToAction("Index", "Companies");
            }

            // If we got this far, something failed; redisplay form.

            return View(companyproductmodel);
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || id <= 0)
                return Globals.Error(400);

            var company = await _context.Companies.SingleOrDefaultAsync(m => m.Id == id);
            if (company == null)
                return Globals.Error(404);

            var vpns = await GetCompanyVpnList(company.Id) ?? new List<VpnConnectionList>();

            var model = new CompanyDetailsViewModel
            {
                Company = company,
                VpNs = vpns
            };

            var x = await GetContacts(id);
            ViewBag.Contacts = x.OrderBy(c => c.FirstName);
            ViewBag.Products = _context.Companies.Join(_context.ProductLinks, c => c.Id, j => j.Cid,
                    (c, j) => new {c, j})
                .Join(_context.Products, @t => @t.j.Pid, p => p.Id, (@t, p) => new {@t, p})
                .Where(@t => @t.@t.c.Id == id.Value)
                .Select(@t => @t.p).ToList();
            ViewBag.Notes = GetCompanyNotes(id.Value);

            ViewBag.Machines = await GetCompanyMachines(id.Value);
            var machineProducts = new List<List<MachineProductsViewModel>>();

            var companymachines = await GetCompanyMachines(id.Value);
            if (companymachines != null)
            {
                var i = 0;
                foreach (var machine in companymachines)
                {
                    machineProducts.Add(new List<MachineProductsViewModel>());
                    var macProducts = await GetMachineProducts(machine.Id);
                    if (macProducts != null)
                    {
                        foreach (var product in macProducts)
                        {
                            machineProducts[i].Add(product);
                        }
                        i++;
                    }
                }
            }
            model.MachineProducts = machineProducts;
            try
            {
                var addresses = await GetCompanyAddresses(id.Value);
                model.Addresses = addresses;
            }
            catch(Exception)
            {
                TempData["notify"] = Globals.Notification(Notification.Error, "Unable to fetch data from the database.");
            }
            return View(model);
        }


        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,CompanyName,Website,Notes")] Company company)
        {
            if (ModelState.IsValid)
            {
                _context.Add(company);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success,
                    $"Successfully added {company.CompanyName}.");
                return RedirectToAction("Details", new {id = company.Id});
            }
            return View(company);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0)
                return Globals.Error(400);

            var company = await _context.Companies.SingleOrDefaultAsync(m => m.Id == id);
            if (company == null || company.Id <= 0)
                return Globals.Error(404);
            TempData["cid"] = company.Id;
            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit()
        {
            var input = HttpContext.Request.Form["CId"];
            int id;
            if (int.TryParse(input, out id) == false)
                return Globals.Error(400);
            if (id == 0)
                return Globals.Error(400);
            if (_context.Companies.Any(c => c.Id == id) == false)
                return Globals.Error(404);
            if (id != Convert.ToInt32(TempData["cid"]))
                return Globals.Error(403);

            var company = new Company
            {
                Id = id,
                CompanyName = HttpContext.Request.Form["name"],
                Website = HttpContext.Request.Form["site"]
            };
            try
            {
                _context.Update(company);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CompanyExists(company.Id))
                    return Globals.Error(401);
                throw;
            }
            return Globals.Error(401);
        }


        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return Globals.Error(401);

            var company = await _context.Companies.SingleOrDefaultAsync(m => m.Id == id);
            if (company == null)
                return Globals.Error(404);

            return View(company);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var company = await _context.Companies.SingleOrDefaultAsync(m => m.Id == id);

            var vpnList = await _context.Vpns.ToListAsync();
            if (vpnList.Any(v => v.Cid == id))
            {
                TempData["notify"] = Globals.Notification(Notification.Error,
                    $"Failed to delete {company.CompanyName} as it still has VPN connections assigned to it");
                return RedirectToAction("Index");
            }

            var macList = await _context.Machines.ToListAsync();
            if (macList.Any(m => m.Cid == id))
            {
                TempData["notify"] = Globals.Notification(Notification.Error,
                    $"Failed to delete {company.CompanyName} as it still has machines assigned to it");
                return RedirectToAction("Index");
            }

            try
            {
                _context.Companies.Remove(company);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success,
                    $"Successfully removed {company.CompanyName} from database.");
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error,
                    $"Failed to remove {company.CompanyName} from database.");
            }

            return RedirectToAction("Index");
        }


        public IActionResult AddProduct(int cid)
        {
            //Product Selections
            var productmodel =
                _context.Products.ToList()
                    .OrderBy(p => p.ProductName)
                    .Select(product => new ProductViewModel {Text = product.ProductName, Value = product.Id.ToString()})
                    .ToList();
            productmodel.Add(new ProductViewModel {Text = "Other", Value = "__"});
            //Get Company
            ViewBag.Comp = _context.Companies.First(c => c.Id == cid).CompanyName;
            ViewBag.cid = cid;
            return View(productmodel);
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct()
        {
            int cid;
            if (!int.TryParse(HttpContext.Request.Form["cid"], out cid))
                return Globals.Error(401);

            var join = new ProductLink
            {
                Cid = cid,
                StartDate = DateTime.ParseExact(HttpContext.Request.Form["daterange"].ToString().Split('-')[0].Trim(),
                    "dd/MM/yyyy", null),
                ExpiryDate = DateTime.ParseExact(HttpContext.Request.Form["daterange"].ToString().Split('-')[1].Trim(),
                    "dd/MM/yyyy", null),
            };

            var version = HttpContext.Request.Form["productVersion"];
            if (!string.IsNullOrWhiteSpace(version))
            {
                join.Version = version;
            }
            var licenseKey = HttpContext.Request.Form["lKey"];
            if (!string.IsNullOrWhiteSpace(licenseKey))
                join.LicenseKey = licenseKey;


            var selForm = HttpContext.Request.Form["options"];
            int pid;
            if (int.TryParse(selForm, out pid))
                join.Pid = pid;
            else
                join.Other = HttpContext.Request.Form["OtherProductName"];

            var company = await GetCompany(join.Cid);

            try
            {
                _context.Add(join);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success,
                    $"Successfully added new product definition to {company.CompanyName}");
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error,
                    $"Failed to add product to {company.CompanyName}");
            }

            return RedirectToAction("Details", new {id = join.Cid});
        }


        public async Task<IActionResult> DeleteProduct(int cid, int pid)
        {
            var productcomp = _context.ProductLinks.Where(j => (j.Cid == cid) && (j.Pid == pid));
            _context.Remove(productcomp.First());
            await _context.SaveChangesAsync();
            var product = _context.Products.Where(p => p.Id == pid).Select(p => p.ProductName).First();
            var company = _context.Companies.Where(c => c.Id == cid).Select(c => c.CompanyName).First();
            TempData["notify"] = "<div class=\"alert alert-success\">" +
                                 "    <a href=\"#\" class=\"close\" data-dismiss=\"alert\" aria-label=\"close\">&times;</a>" +
                                 $"   <strong>Success!</strong> {product} removed from {company}." +
                                 "</div>";
            return RedirectToAction("Details", new {id = cid});
        }


        public async Task<IActionResult> EditNotes(int id)
        {
            if (!await CompanyExists(id))
            {
                return RedirectToAction("Index", "Error", new {id = 404});
            }
            ViewBag.Content = GetCompanyNotes(id);
            var company = _context.Companies.First(c => c.Id == id);
            TempData["id"] = id;
            return View(company);
        }

        [HttpPost]
        public IActionResult EditNotes()
        {
            var content = HttpContext.Request.Form["editor"];
            var id = TempData["id"];
            SaveCompanyNotes(Convert.ToInt32(id), content);
            return RedirectToAction("Details", new {id});
        }


        public async Task<IActionResult> AddMachine(int? id)
        {
            if (id == null || id <= 0)
                return Globals.Error(400);
            var z = await GetCompany(id.Value);
            if (z != null)
            {
                var x = await GetCompany(id.Value);
                ViewBag.Company = x.CompanyName;
            }
            else
            {
                return Globals.Error(404);
            }
            ViewBag.cid = id;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddMachine()
        {
            int cid;
            if (!int.TryParse(HttpContext.Request.Form["cid"], out cid))
                return Globals.Error(403);

            var machine = new Machine
            {
                Type = HttpContext.Request.Form["Type"],
                ModelNumber = HttpContext.Request.Form["MNumber"],
                Cid = cid,
                SerialKey = HttpContext.Request.Form["sKey"],
                HostName = HttpContext.Request.Form["hName"]
            };

            try
            {
                _context.Add(machine);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success, "Successfully added machine.");
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error,
                    "Unable to add machine to database, please try again.");
            }

            return RedirectToAction("Index", "Machine", new {id = machine.Id});
        }


        public async Task<IActionResult> AddVpn(int? id)
        {
            if (id == null)
                return RedirectToAction("Index", "Error", new {id = 404});
            if (!await CompanyExists(id.Value))
                return RedirectToAction("Index", "Error", new {id = 404});
            ViewBag.cid = id.Value;
            ViewBag.company = (await GetCompany(id.Value)).CompanyName;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddVpn()
        {
            var inCid = HttpContext.Request.Form["cid"];
            int cid;
            if (!int.TryParse(inCid, out cid))
                return RedirectToAction("Index", "Error", new {id = 401});

            var vpn = new Vpn
            {
                Address = HttpContext.Request.Form["address"],
                Cid = cid,
                Type = HttpContext.Request.Form["type"]
            };

            try
            {
                var c = await GetCompany(vpn.Cid);
                _context.Add(vpn);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success,
                    $"Successfully added {vpn.Type} {vpn.Address} to {c.CompanyName}");
            }
            catch (Exception e)
            {
                TempData["notify"] = Globals.Notification(Notification.Error,
                    "There was an error adding new VPN connection to the database, please try again.");
                Globals.SendError(e);
            }
            return RedirectToAction("Details", new {id = cid});
        }


        public async Task<IActionResult> AddCredentials(int? id)
        {
            if (id == null || id <= 0)
                return Globals.Error(400);

            var vExists = await VpnExists(id.Value);
            if (!vExists)
                return Globals.Error(404);
            var vpn = await GetVpn(id.Value);

            var cExists = await CompanyExists(vpn.Cid);
            if (!cExists)
                return RedirectToAction("Index", "Error", new {id = 404});
            var company = await GetCompany(vpn.Cid);

            var model = new AddCredentialsViewModel
            {
                Company = company,
                Vpn = vpn
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddCredentials()
        {
            int vpnId;
            if (!int.TryParse(HttpContext.Request.Form["vId"], out vpnId))
                return RedirectToAction("Index", "Error", new {id = 401});

            var exists = await VpnExists(vpnId);
            if (!exists)
                return RedirectToAction("Index", "Error", new {id = 401});

            var vpn = await GetVpn(vpnId);
            var company = await GetCompany(vpn.Cid);

            var creds = new VpnCredentials
            {
                VpnId = vpnId,
                Username = HttpContext.Request.Form["usr"],
                Password = HttpContext.Request.Form["pwd"]
            };

            try
            {
                _context.Add(creds);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success,
                    $"Successfully added credentials to {company.CompanyName} {vpn.Type}");
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error,
                    "Failed to add credentials please try again");
            }

            return RedirectToAction("Details", new {id = vpn.Cid});
        }


        public async Task<IActionResult> DeleteCredentials(int? cid)
        {
            if (cid == null)
                return RedirectToAction("Index", "Error", new {id = 401});

            var exists = await CredentialsExist(cid.Value);
            if (!exists)
                return RedirectToAction("Index", "Error", new {id = 401});

            var credsList = await _context.VpnCredentials.ToListAsync();
            var creds = credsList.First(c => c.Id == cid);
            var vpn = await GetVpn(creds.VpnId);

            ViewBag.compId = vpn.Cid;
            ViewBag.cid = cid;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCredentials()
        {
            var cidIn = HttpContext.Request.Form["cid"];
            int cid;
            if (!int.TryParse(cidIn, out cid))
                return RedirectToAction("Index", "Error", new {id = 401});

            var exists = await CredentialsExist(cid);
            if (!exists)
                return RedirectToAction("Index", "Error", new {id = 404});

            var credsList = await _context.VpnCredentials.ToListAsync();
            var creds = credsList.First(c => c.Id == cid);

            try
            {
                _context.Remove(creds);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success,
                    "Successfully removed credentials from the database");
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error,
                    "Failed to remove credentials from the database.");
            }

            var vpnList = await _context.Vpns.ToListAsync();
            var vpn = vpnList.First(v => v.Id == creds.VpnId);
            return RedirectToAction("Details", new {id = vpn.Cid});
        }


        public async Task<IActionResult> EditVpn(int? id)
        {
            if (id == null)
                return RedirectToAction("Index", "Error", new {id = 401});
            var exists = await VpnExists(id.Value);
            if (!exists)
                return RedirectToAction("Index", "Error", new {id = 404});

            var model = await GetVpn(id.Value);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditVpn()
        {
            var address = HttpContext.Request.Form["address"];
            var vpnIdIn = HttpContext.Request.Form["vId"];

            int vpnId;
            if (!int.TryParse(vpnIdIn, out vpnId))
                return RedirectToAction("Index", "Error", new {id = 401});

            var vpnExists = await VpnExists(vpnId);
            if (!vpnExists)
                return RedirectToAction("Index", "Error", new {id = 404});

            var vpn = await GetVpn(vpnId);
            vpn.Address = address;

            try
            {
                _context.Update(vpn);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success, "Successfully updated hostname.");
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error, "Error failed to edit hostname.");
            }
            return RedirectToAction("Details", new {id = vpn.Cid});
        }


        public async Task<IActionResult> EditVpnNotes(int? id)
        {
            if (id == null)
                return RedirectToAction("Index", "Error", new {id = 401});
            var exists = await VpnExists(id.Value);
            if (!exists)
                return RedirectToAction("Index", "Error", new {id = 404});

            ViewBag.notes = GetVpnNotes(id.Value);
            ViewBag.id = id.Value;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EditVpnNotes()
        {
            var vpnIdIn = HttpContext.Request.Form["vId"];

            int vpnId;
            if (!int.TryParse(vpnIdIn, out vpnId))
                return RedirectToAction("Index", "Error", new {id = 401});

            var exists = await VpnExists(vpnId);
            if (!exists)
                return RedirectToAction("Index", "Error", new {id = 404});
            var vpn = await GetVpn(vpnId);

            try
            {
                SaveVpnNotes(vpnId, HttpContext.Request.Form["editor"]);
                TempData["notify"] = Globals.Notification(Notification.Success, "Successfully saved notes to file");
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error, "Error saving notes to file");
            }
            return RedirectToAction("Details", new {id = vpn.Cid});
        }


        public async Task<IActionResult> DeleteVpn(int? id)
        {
            if (id == null)
                return RedirectToAction("Index", "Error", new {id = 401});

            var exists = await VpnExists(id.Value);
            if (!exists)
                return RedirectToAction("Index", "Error", new {id = 404});

            var model = await GetVpn(id.Value);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteVpn()
        {
            var idIn = HttpContext.Request.Form["vId"];
            int id;

            if (!int.TryParse(idIn, out id))
                return RedirectToAction("Index", "Error", new {id = 401});

            var exists = await VpnExists(id);
            if (!exists)
                return RedirectToAction("Index", "Error", new {id = 404});

            var vpn = await GetVpn(id);
            try
            {
                _context.Remove(vpn);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success, "Successfully removed VPN from database");
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error,
                    "Unable to remove VPN connection from database");
            }
            return RedirectToAction("Details", new {id = vpn.Cid});
        }

        public async Task<IActionResult> AddAddress(int? cid)
        {
            if (cid == null || cid == 0 || !await CompanyExists(cid.Value))
                return RedirectToAction("Index", "Error", "404");
            var model = new AddressFormViewModel
            {
                Company = await GetCompany(cid.Value),
                Addresses = await GetCompanyAddresses(cid.Value)
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddAddress()
        {
            int cid, num;
            if (!int.TryParse(HttpContext.Request.Form["cid"], out cid) 
                || !int.TryParse(HttpContext.Request.Form["num"], out num))
                return RedirectToAction("Index", "Error", new {id = 404});
            var address = new Address
            {
                Cid = cid,
                Number = num,
                Street = HttpContext.Request.Form["street"],
                County = HttpContext.Request.Form["county"],
                City = HttpContext.Request.Form["city"],
                PostCode = HttpContext.Request.Form["postcode"]
            };
            try
            {
                _context.Add(address);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success,
                    "Successfully added new address to database.");
                return RedirectToAction("Details", new {id = cid});
            }
            catch (Exception e)
            {
                Globals.SendError(e);
                TempData["notify"] = Globals.Notification(Notification.Error,
                    "Unable to add Address to the database.");
            }
            return RedirectToAction("Index","Error", new {id = 404});
        }

        public async Task<IActionResult> DeleteAddress(int aId)
        {
            if (aId >= 1)
            {
                if (await AddressExists(aId))
                {
                    var address = await GetAddress(aId);
                    try
                    {
                        _context.Remove(address);
                        await _context.SaveChangesAsync();
                        TempData["notify"] = Globals.Notification(Notification.Success,
                            "Address successfully deleted from the database.");
                    }
                    catch (Exception)
                    {
                        TempData["notify"] = Globals.Notification(Notification.Error,
                            "Unable to add address to database please try again later.");
                    }
                    return RedirectToAction("Details", new {id = address.Cid});
                }
            }
            TempData["notify"] = Globals.Notification(Notification.Warning,
                "Address does not exist or the address is unknown");
            return RedirectToAction("Index", "Error", new {id = 404});
        }

        #region Methods
        
        //Returns a company address with a matching primary key.

        private async Task<Address> GetAddress(int AId)
        {
            var list = await _context.Addresses.ToListAsync();
            if (AId < 1) throw new Exception("Invalid Address Id");
            if(list.Any(a => a.Id == AId))
            {
                return list.First(a => a.Id == AId);
            }
            throw new Exception("Invalid Address Id");
        }

        //Returns true if an address witha matching primary key exists in the database.
        private async Task<bool> AddressExists(int aId)
        {
            var addressList = await _context.Addresses.ToListAsync();
            return addressList.Any(a => a.Id == aId);
        }

        //Returns true if a company with a matching primary key exists in the database.
        private async Task<bool> CompanyExists(int id)
        {
            var comList = await _context.Companies.ToListAsync();
            return comList.Any(e => e.Id == id);
        }

        //Returns true if a vpn with a matching primary key exists in the database.
        private async Task<bool> VpnExists(int id)
        {
            var vpnList = await _context.Vpns.ToListAsync();
            return vpnList.Any(v => v.Id == id);
        }

        //Returns true if a credentials set with a matching primary key exists in the database.
        private async Task<bool> CredentialsExist(int id)
        {
            var credsList = await _context.VpnCredentials.ToListAsync();
            return credsList.Any(c => c.Id == id);
        }

        //Gets a list of contacts of a company given a Company Id.
        private async Task<IEnumerable<Contact>> GetContacts(int? cid)
        {
            var conList = await _context.Contacts.ToListAsync();
            return conList.Where(c => c.Cid == cid);
        }

        //Returns a product based on a product Id.
        private async Task<Product> GetProduct(int pid)
        {
            var prodList = await _context.Products.ToListAsync();
            return prodList.FirstOrDefault(p => p.Id == pid);
        }

        //Returns a company based on a company Id.
        private async Task<Company> GetCompany(int id)
        {
            var compList = await _context.Companies.ToListAsync();
            return compList.FirstOrDefault(c => c.Id == id);
        }

        //Returns a vpn based on a vpn Id.
        private async Task<Vpn> GetVpn(int id)
        {
            var vpnList = await _context.Vpns.ToListAsync();
            return vpnList.FirstOrDefault(v => v.Id == id);
        }

        //Returns a list of Addresses with a matching company cid
        private async Task<List<Address>> GetCompanyAddresses(int cid)
        {
            var addressList = await _context.Addresses.ToListAsync();
            var addresses = addressList.FindAll(a => a.Cid == cid);
            return addresses;
        }

        //Company Notes
        private static string GetCompanyNotes(int id)
        {
            string text;
            try
            {
                text = System.IO.File.ReadAllText(Globals.BasePath + "\\Data\\Companies\\Notes\\" + id + ".txt");
            }
            catch
            {
                text = string.Empty;
            }
            return text;
        }

        private static void SaveCompanyNotes(int id, string content)
        {
            try
            {
                System.IO.File.WriteAllText(Globals.BasePath + "\\Data\\Companies\\Notes\\" + id + ".txt", content);
            }
            catch (Exception e)
            {
                Globals.SendError(e);
            }
        }

        //VPN Notes
        private static string GetVpnNotes(int id)
        {
            string text;
            try
            {
                text = System.IO.File.ReadAllText(Globals.BasePath + "\\Data\\VPNs\\Notes\\" + id + ".txt");
            }
            catch
            {
                text = string.Empty;
            }
            return text;
        }

        private static void SaveVpnNotes(int id, string content)
        {
            try
            {
                System.IO.File.WriteAllText(Globals.BasePath + "\\Data\\VPNs\\Notes\\" + id + ".txt", content);
            }
            catch (Exception e)
            {
                Globals.SendError(e);
            }
        }

        //Get machines assigned to a company based on a company Id.
        private async Task<List<Machine>> GetCompanyMachines(int id)
        {
            var mList = await _context.Machines.ToListAsync();
            var retList = new List<Machine>();
            if (mList.Any(m => m.Cid == id))
            {
                retList.AddRange(mList.Where(m => m.Cid == id).ToList());
                return retList;
            }
            return new List<Machine>();
        }

        /* Gets List of MachineProductsViewModel that contains all products including products with no id's
         * relating to a given machine found by Id.*/

        private async Task<IEnumerable<MachineProductsViewModel>> GetMachineProducts(int id)
        {
            var linksList = await _context.ProductLinks.ToListAsync();
            var prodList = await _context.Products.ToListAsync();
            if (linksList.Any(m => m.Mid == id))
            {
                return linksList.Where(l => l.Mid == id).Select(
                    link => new MachineProductsViewModel
                    {
                        Id = link.Id,
                        Expires = link.ExpiryDate,
                        LicenseKey = link.LicenseKey,
                        Version = link.Version,
                        ProductName = link.Pid == null ? link.Other : prodList.First(p => p.Id == link.Pid).ProductName
                    }).ToList();
            }
            return new List<MachineProductsViewModel>();
        }

        //Returns true if a company has any expired products
        private async Task<bool> GetExpired(int cid)
        {
            var machines = await GetCompanyMachines(cid);
            if (machines != null)
            {
                foreach (var machine in machines)
                {
                    if (_context.ProductLinks.Any(l => l.Mid == machine.Id))
                    {
                        var lList = await _context.ProductLinks.ToListAsync();
                        if (
                            lList.Any(
                                l =>
                                    l.ExpiryDate <
                                    DateTime.ParseExact(DateTime.Now.Date.ToString("dd-MM-yy"), "dd-MM-yy", null)))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        //Generates ProductViewModel list for displaying products on a page.
        private async Task<List<ProductViewModel>> GetProdList()
        {
            var prodlist = await _context.Products.ToListAsync();
            return prodlist.Select(p => new ProductViewModel
            {
                Text = p.ProductName,
                Value = p.Id.ToString()
            }).ToList();
        }

        //Generates CompanyViewModel list for displaying companies on a page.
        private async Task<List<CompanyViewModel>> GetCompList()
        {
            var cList = await _context.Companies.ToListAsync();
            var retList = new List<CompanyViewModel>();
            foreach (var c in cList)
            {
                var x = new CompanyViewModel
                {
                    Id = c.Id,
                    Name = c.CompanyName,
                    Website = c.Website,
                    Expired = await GetExpired(c.Id)
                };
                retList.Add(x);
            }
            return retList.OrderBy(c => c.Name).ToList();
        }

        //Queries for Index page, using CompanyViewModel.

        #region QueryMethods

        //Searches through a CompanyViewModel List and returns entries whose names contain the given searchstring.
        private static List<CompanyViewModel> SearchQuery(IEnumerable<CompanyViewModel> list, string searchString)
        {
            return list.Where(c => c.Name.ToLower().Contains(searchString.ToLower())).ToList();
        }

        //Searches through a CompanyViewModel and returns only those were GetExpire cid returns true.
        private async Task<List<CompanyViewModel>> ExpiredQuery(IEnumerable<CompanyViewModel> inList)
        {
            var list = inList.ToList();
            var retList = new List<CompanyViewModel>();
            if (list.Any())
            {
                foreach (var company in list)
                {
                    if (await GetExpired(company.Id))
                    {
                        retList.Add(company);
                    }
                }
                return retList;
            }
            return list;
        }

/*Gets all companies from a database who have the product that matches the input id, and converts
                                     * them into a CompanyViewModel List.*/

        private async Task<List<CompanyViewModel>> ProductIdQuery(int productid)
        {
            var retList = new List<CompanyViewModel>();
            var compL = await _context.Companies.ToListAsync();
            var linkL = await _context.ProductLinks.ToListAsync();
            var prodL = await _context.Products.ToListAsync();
            var range = (from c in compL
                join l in linkL on c.Id equals l.Cid
                join p in prodL on l.Pid equals p.Id
                where l.Pid == productid
                select c).ToList();
            foreach (var c in range)
            {
                var add = new CompanyViewModel
                {
                    Id = c.Id,
                    Website = c.Website,
                    Name = c.CompanyName
                };
                add.Expired = await GetExpired(add.Id);
                retList.Add(add);
            }
            return retList.ToList();
        }

        #endregion

        //Returns a list of VPN connection details with credentials for each.
        private async Task<List<VpnConnectionList>> GetCompanyVpnList(int cid)
        {
            var vpnList = await _context.Vpns.ToListAsync();
            var credList = await _context.VpnCredentials.ToListAsync();

            var retList = new List<VpnConnectionList>();

            if (vpnList.Any(v => v.Cid == cid))
            {
                retList.AddRange(from vpn in vpnList.Where(v => v.Cid == cid)
                    let creds = credList.Where(c => c.VpnId == vpn.Id).ToList()
                    select new VpnConnectionList
                    {
                        Vpn = vpn, CredList = creds, VpnNotes = GetVpnNotes(vpn.Id)
                    });
                return retList;
            }
            return null;
        }

        #endregion
    }
}