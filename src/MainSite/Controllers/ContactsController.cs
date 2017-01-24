#region Usings

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MainSite.Data;
using MainSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

#endregion

namespace MainSite.Controllers
{
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Contacts
        public IActionResult Index(string searchstring)
        {
            IEnumerable<Contact> conList = _context.Contacts.ToList();
            if (!string.IsNullOrWhiteSpace(searchstring)) conList = SearchQuery(conList, searchstring);

            //Set View Data
            ViewBag.Companies = _context.Companies.ToList();
            ViewBag.Contacts = conList;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index()
        {
            IEnumerable<Contact> conList = _context.Contacts.ToList();
            var searchstring = HttpContext.Request.Form["searchinput"];
            if (!string.IsNullOrWhiteSpace(searchstring)) conList = SearchQuery(conList, searchstring);
            ViewBag.Companies = _context.Companies.ToList();
            ViewBag.Contacts = conList;
            return View();
        }

        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var contact = await _context.Contacts.SingleOrDefaultAsync(m => m.Id == id);
            if (contact == null)
                return NotFound();
            ViewBag.Company = _context.Companies.First(c => c.Id == contact.Cid).CompanyName;
            return View(contact);
        }

        // GET: Contacts/Create
        public IActionResult Create(int? cid)
        {
            if (cid == null)
                return RedirectToAction("Index", "Error", new {id = 400});
            if (_context.Companies.Any(c => c.Id == cid) == false)
                return RedirectToAction("Index", "Error", new {id = 404});
            var query = _context.Companies.First(c => c.Id == cid);
            ViewBag.Comp = query.CompanyName;
            ViewBag.t = $"Add Contact to {query.CompanyName}";
            ViewBag.cid = query.Id;
            return View();
        }

        // POST: Contacts/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create( [Bind("Id,CID,FirstName,LastName,Email,MobilePhone,Phone")] Contact contact)
        {
            if (ContactExists(contact.Id))
            {
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                var cid = HttpContext.Request.Form["cid"];
                int output;
                if (!int.TryParse(cid, out output))
                    return Globals.Error(401);
                contact.Cid = output;
                _context.Add(contact);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Companies", new {id = output});
            }
            return View(contact);
        }

        // GET: Contacts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return Globals.Error(404);

            var contact = await _context.Contacts.SingleOrDefaultAsync(m => m.Id == id);
            if (contact == null)
                return Globals.Error(404);
            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,CID,Email,FirstName,LastName,MobilePhone,Phone")] Contact contact)
        {
            if (id != contact.Id)
                return Globals.Error(404);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(contact);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
                        return Globals.Error(404);
                    throw;
                }
                return Globals.Error(404);
            }
            return View(contact);
        }

        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return Globals.Error(404);

            var contact = await _context.Contacts.SingleOrDefaultAsync(m => m.Id == id);
            if (contact == null)
                return Globals.Error(404);

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contact = await _context.Contacts.SingleOrDefaultAsync(m => m.Id == id);
            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        private bool ContactExists(int id)
        {
            return _context.Contacts.Any(e => e.Id == id);
        }

        private IEnumerable<Contact> SearchQuery(IEnumerable<Contact> inList, string searchstring)
        {
            var comp = from c in _context.Companies select c;
            /*var retList = from c in inList
                          let full = c.FirstName + c.LastName
                          where full.Contains(searchstring)
                          select c;*/
            var retList = from c in inList
                join x in _context.Companies on c.Cid equals x.Id
                let full = c.FirstName + c.LastName
                where
                full.ToLower().Contains(searchstring.ToLower()) ||
                x.CompanyName.ToLower().Contains(searchstring.ToLower())
                select c;

            return retList;
        }
    }
}