#region Usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MainSite.Data;
using MainSite.Debug;
using MainSite.Models;
using MainSite.Models.DocumentationViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

#endregion

namespace MainSite.Controllers
{
    [Authorize]
    public class DocumentationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DocumentationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Documentation
        [HttpGet]
        public IActionResult Index()
        {
            var model = new CategoryViewModel {CategoriesList = new List<Category>()};
            model.CategoriesList.AddRange(_context.Categories.ToList());

            var docList =
                _context.Documentations.Select(doc =>
                            new DocumentViewModel
                            {
                                Category = (from c in _context.Categories
                                            where c.Id == doc.CgId
                                            select c.CategoryName).First(),
                                Id = doc.Id,
                                Title = doc.Title
                            }).ToList();
            ViewBag.docList = docList;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CategoryViewModel model)
        {
            model.CategoriesList.AddRange(_context.Categories.ToList().OrderBy(c => c.CategoryName));

            var documents = await _context.Documentations.ToListAsync();
            var formCon = HttpContext.Request.Form["searchInput"];
            var input = HttpContext.Request.Form["selection"];
            if (input != "*" || !string.IsNullOrWhiteSpace(formCon))
            {
                documents = Query(documents, input, formCon);
            }
            else
            {
                return RedirectToAction("Index");
            }
            ViewBag.docList = documents.Select(doc =>
                            new DocumentViewModel
                            {
                                Category = (from c in _context.Categories
                                    where c.Id == doc.CgId
                                    select c.CategoryName).First(),
                                Id = doc.Id,
                                Title = doc.Title
                            }).ToList().OrderBy(d => d.Title);
            return View(model);
            
        }

        // GET: Documentation/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return Globals.Error(404);
            }

            var documentation = await _context.Documentations.SingleOrDefaultAsync(m => m.Id == id);
            if (documentation == null)
            {
                return Globals.Error(404);
            }

            return View(documentation);
        }

        // GET: Documentation/Create
        public IActionResult Create()
        {
            //Initialize Category selection list.
            var model = new CategoryViewModel {CategoriesList = new List<Category>()};
            model.CategoriesList.AddRange(_context.Categories.ToList().OrderBy(c => c.CategoryName));

            return View(model);
        }

        // POST: Documentation/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Category,Creator,FileName,Title")] Documentation documentation)
        {
            if (ModelState.IsValid)
            {
                documentation.Title = HttpContext.Request.Form["Title"];
                documentation.CgId =
                (from c in _context.Categories
                    where c.Id == Convert.ToInt32(HttpContext.Request.Form["Category"])
                    select c).First().Id;
                documentation.FileName = FileName(documentation.Title);
                documentation.Creator = _userManager.GetUserId(User);
                _context.Add(documentation);
                await _context.SaveChangesAsync();
                TempData["notify"] = Globals.Notification(Notification.Success, 
                    $"<strong>Success!</strong> Document <strong>{documentation.Title}</strong> was created successfully.");
                System.Diagnostics.Debug.WriteLine($"New document created ({documentation.Title}) by {_userManager.GetUserName(User)}.");
                SaveDocument(documentation.Id, string.Empty);
                return RedirectToAction("Document", new {id = documentation.Id});
            }
            TempData["notify"] = Globals.Notification(Notification.Error, 
                $"<strong>Failed!</strong> Failed to add document <strong>{documentation.Title}</strong> to the database due to invalid model state.");

            var model = new CategoryViewModel {CategoriesList = new List<Category>()};
            model.CategoriesList.AddRange(_context.Categories.ToList().OrderBy(c => c.CategoryName));
            return View(model);
        }

        // GET: Documentation/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0)
            {
                return Globals.Error(400);
            }

            var documentation = await _context.Documentations.SingleOrDefaultAsync(m => m.Id == id);
            if (documentation == null)
            {
                return Globals.Error(404);
            }
            ViewBag.Content = GetDocText(
                _context.Documentations.Where(d => d.Id == id).Select(d => d.FileName).First(),
                _context.Categories.Join(_context.Documentations, c => c.Id, d => d.CgId, (c, d) => c.Directory).First());
            ViewBag.t = _context.Documentations.Where(d => d.Id == id).Select(d => d.Title).First();
            return View(documentation);
        }

        // POST: Documentation/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FileName,Title,CGId")] Documentation documentation)
        {
            if (id != documentation.Id)
            {
                return Globals.Error(404);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    documentation.Creator =
                        _context.Documentations.Where(d => d.Id == documentation.Id).Select(d => d.Creator).First();
                    _context.Update(documentation);
                    await _context.SaveChangesAsync();
                    SaveDocument(documentation.Id, HttpContext.Request.Form["editor"]);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DocumentationExists(documentation.Id))
                    {
                        return Globals.Error(404);
                    }
                    throw;
                }
                return View(documentation);
            }
            return Globals.Error(400);
        }

        // GET: Documentation/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var documentation = await _context.Documentations.SingleOrDefaultAsync(m => m.Id == id);
            if (documentation == null)
            {
                return NotFound();
            }

            return View(documentation);
        }

        // POST: Documentation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var documentation = await _context.Documentations.SingleOrDefaultAsync(m => m.Id == id);
            _context.Documentations.Remove(documentation);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Document(int id)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index", "Error", new {id = 404});
            }
            var document = await _context.Documentations.SingleOrDefaultAsync(d => d.Id == id);
            if (document == null)
            {
                return RedirectToAction("Index", "Error", new {id = 404});
            }
            ViewBag.User = _userManager.FindByIdAsync(document.Creator).Result.UserName;
            ViewBag.Text = GetDocText(document.FileName, (from c in _context.Categories where c.Id == document.CgId select c.Directory).First());
            ViewBag.T = _context.Documentations.Where(d => d.Id == id).Select(d => d.Title).First();
            ViewBag.Id = id;
            if (ViewBag.Text == null)
            {
                return RedirectToAction("Index", "Error", new {id = 404});
            }
            return View();
        }


        public IActionResult Categories()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        public async Task<IActionResult> Categories_Delete(int? id)
        {
            if (id == null)
                return RedirectToAction("Index", "Error", new {id = 404});
            var category = await _context.Categories.SingleOrDefaultAsync(c => c.Id == id);
            if (category == null)
                return RedirectToAction("Index", "Error", new {id = 404});
            var checkcategory =
            (from d in _context.Documentations
                join c in _context.Categories on d.CgId equals c.Id
                where c.Id == id.Value
                select d).ToList();
            if (checkcategory.Count > 0 || !CategoryExist(id.Value))
            {
                TempData["notify"] = Globals.Notification(Notification.Error, 
                    $"<strong>Failed!</strong> Could not remove the category <strong>{category.CategoryName}</strong> from the database, due to the category still having documents assigned to it.");
                return RedirectToAction("Categories");
            }
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["notify"] = Globals.Notification(Notification.Success, 
                $"<strong>Success!</strong> Removed <strong>{category.CategoryName}</strong> category from the database.");
            return RedirectToAction("Categories");
        }

        public IActionResult Add_Category()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add_Category([Bind("ID", "CategoryName")] Category cat)
        {
            cat.Directory = cat.CategoryName.Replace(' ', '_');
            var category = new Category(cat.Id, cat.CategoryName, cat.Directory);
            try
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                Directory.CreateDirectory(Path.Combine(Globals.BasePath + "\\Data\\Documentation\\" + category.Directory));
                TempData["notify"] = Globals.Notification(Notification.Success, "Successfully created category.");
            }
            catch
            {
                TempData["notify"] = Globals.Notification(Notification.Error, "Error creating category please try again");
            }
            return RedirectToAction("Categories");
        }

        #region Methods

        //Methods
        private void SaveDocument(int id, string content)
        {
            var categoryNorm = _context.Documentations.Join(_context.Categories, d => d.CgId, c => c.Id,
                (d, c) => new {d, c}).Where(t => t.d.Id == id).Select(t => t.c.Directory).First();
            var documentFileName = _context.Documentations.Where(d => d.Id == id).Select(d => d.FileName).First();
            var path = Globals.BasePath + "\\Data\\Documentation\\" + categoryNorm + "\\" + documentFileName;
            System.IO.File.WriteAllText(path, content);
        }

        private bool DocumentationExists(int id)
        {
            return _context.Documentations.Any(e => e.Id == id);
        }

        private bool CategoryExist(int id)
        {
            return _context.Categories.Any(c => c.Id == id);
        }

        private static string FileName(string documentationTitle)
        {
            return documentationTitle.Replace(' ', '_') + ".txt";
        }

        private static string GetDocText(string documentFileName, string categoryNorm)
        {
            try
            {
                var path = Globals.BasePath + "\\Data\\Documentation\\" + categoryNorm + "\\" + documentFileName;
                return System.IO.File.ReadAllText(path);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private List<Documentation> Query(List<Documentation> documents, string input, string search)
        {
            int selection;
            if (int.TryParse(input, out selection) && !string.IsNullOrWhiteSpace(search))
            {
                documents = (from d in documents
                             join c in _context.Categories on d.CgId equals c.Id
                             where d.CgId == selection
                             where d.Title.ToLower().Contains(search.ToLower())
                             select d).ToList();
            }
            else if (!string.IsNullOrWhiteSpace(search))
            {
                documents = (from d in documents
                             where d.Title.ToLower().Contains(search.ToLower())
                             select d).ToList();
            }
            else if (int.TryParse(input, out selection))
            {
                documents = (from d in documents
                             join c in _context.Categories on d.CgId equals c.Id
                             where d.CgId == selection
                             select d).ToList();
            }
            return documents;
        }
        #endregion
    }
}