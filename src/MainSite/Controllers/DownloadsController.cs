using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MainSite.Data;
using MainSite.Models.DownloadViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace MainSite.Controllers
{
    public class DownloadsController : Controller
    {
        public IActionResult Index()
        {
            var model = DownloadData();
            return View(model);
        }
        [HttpPost]
        public IActionResult Index(DownloadViewModel model)
        {
            var selectionIn = HttpContext.Request.Form["selection"].ToString();
            var search = HttpContext.Request.Form["search"].ToString();
            int selection;
            if (int.TryParse(selectionIn, out selection) || search != null)
            {
                var viewModel = DownloadData();
                if(int.TryParse(selectionIn, out selection))
                {
                    viewModel.Downloads = viewModel.Downloads.Where(d => d.CatId == selection).OrderBy(d => d.Name).ToList();
                }
                if (search != string.Empty)
                {
                    viewModel.Downloads = viewModel.Downloads.Where(d => d.Name.ToLower().Contains(search.ToLower())).OrderBy(d => d.Name).ToList();
                }
                return View(viewModel);
            }
            return View(model);
        }


        public IActionResult Upload(int? x)
        {
            var model = DownloadData().Categories;
            model = model.OrderBy(c => c.CategoryName).ToList();
            model.Add(new DownloadCategory
            {
                Id = 0,
                CategoryName = "New Category"
            });
            return View(model);
        }

        [HttpPost]
        [RequestFormSizeLimit(2147483647)]
        public async Task<IActionResult> Upload(ICollection<IFormFile> files)
        {
            var id = Convert.ToInt32(HttpContext.Request.Form["category"]);
            DownloadCategory category;

            if (id == 0)
            {
                var name = HttpContext.Request.Form["categoryForm"].First();
                if (Globals.PathInvalids.Any(invalid => name.Contains(invalid)))
                {
                    TempData["notify"] = Globals.Notification(Notification.Error, "Please only use valid path characters");
                    return RedirectToAction("Upload");
                }
                category = new DownloadCategory(name);
            }
            else
            {
                category = new DownloadCategory();
                var directories = Directory.GetDirectories(Globals.BasePath + "\\Data\\Downloads\\");
                foreach (var directory in directories)
                {
                    if (directory.Split('\\').Last().Split('_')[0] == id.ToString())
                    {
                        category.CategoryName = directory.Split('\\').Last().Split('_')[1];
                    }
                }
                category.Id = id;
            }

            var file = HttpContext.Request.Form.Files[0];
            if (file == null)
            {
                TempData["notify"] = Globals.Notification(Notification.Error, "File got lost somewhere in post request");
                return RedirectToAction("Upload");
            }
            if (Globals.PathInvalids.Any(invalid => file.Name.Contains(invalid)))
            {
                TempData["notify"] = Globals.Notification(Notification.Error, "Please only use valid path characters");
                return RedirectToAction("Upload");
            }
            if (file?.Length > 1)
            {
                using (var fs = new FileStream(
                    $"{Globals.BasePath}\\Data\\Downloads\\{category.Id}_{category.CategoryName}\\{file.FileName}", FileMode.Create))
                {
                    try
                    {
                        await file.CopyToAsync(fs);
                    }
                    catch (InvalidDataException)
                    {
                        TempData["notify"] = Globals.Notification(Notification.Error, "File limit exceeded maximum file length equals 128 Megabytes (134217728 bytes), if your file exceeds this limit please contact someone about putting the file on the server.");
                    }
                }
            }
            return RedirectToAction("Index");
        }


        public IActionResult Delete(string name, int catId)
        {
            if (!string.IsNullOrWhiteSpace(name) || catId != 0)
            {
                var model = new DownloadViewModel
                {
                    Categories = new List<DownloadCategory>(),
                    Downloads = new List<Download>()
                };
                
                var directories = Directory.GetDirectories(Globals.BasePath + "\\Data\\Downloads\\");
                var category = GetCategory(catId);
                if (category != null)
                {
                    model.Categories.Add(category);
                    var files = Directory.GetFiles(Globals.BasePath + "\\Data\\Downloads\\" + category.Id + "_" +
                                           category.CategoryName);
                    if (files.Any(file => file.Split('\\').Last() == name))
                    {
                        model.Downloads.Add(new Download
                        {
                            CatId = catId,
                            Name = name,
                            Url = $"ftp://{HttpContext.Request.Host}/{category.Id}_{category.CategoryName}/{name}"
                        });
                        return View(model);
                    }
                }
            }
            return RedirectToAction("Index", "Error", new {id = 404});
        }

        [HttpPost]
        public IActionResult Delete()
        {
            var file = HttpContext.Request.Form["name"];
            var catId = HttpContext.Request.Form["catId"];
            var directories = Directory.GetDirectories($"{Globals.BasePath}\\Data\\Downloads");
            foreach (var dir in directories)
            {
                if (dir.Split('\\').Last().Split('_')[0] == catId)
                {
                    var files = Directory.GetFiles(dir);
                    foreach (var f in files)
                    {
                        if (f.Split('\\').Last() == file)
                        {
                            System.IO.File.Delete(f);
                            TempData["notify"] = Globals.Notification(Notification.Success, $"Sucessfully deleted {file} from server.");
                            return RedirectToAction("Index");
                        }
                    }
                }
            }
            TempData["notify"] = Globals.Notification(Notification.Info, "Unable to delete file or file not found");
            return RedirectToAction("Index");
        }


        #region Methods

        public DownloadViewModel DownloadData()
        {
            var downloads = new List<Download>();
            var categories = new List<DownloadCategory>();
            var directories = Directory.GetDirectories(Globals.BasePath + "\\Data\\Downloads\\");
            foreach (var directory in directories)
            {
                var catDir = Path.GetFullPath(directory).Split('\\').Last();
                categories.Add(new DownloadCategory
                {
                    CategoryName = catDir.Split('_')[1],
                    Id = Convert.ToInt32(catDir.Split('_')[0])
                });
                downloads.AddRange(from file in Directory.GetFiles(directory)
                    let d = Path.GetDirectoryName(file)
                    let url = "ftp://192.168.10.180/" + d.Split('\\').Last() + "/" + Path.GetFileName(file)
                    select new Download
                    {
                        CatId = Convert.ToInt32(d.Split('\\').Last().Split('_')[0]), Name = Path.GetFileName(file), Url = url
                    });
            }
            var model = new DownloadViewModel
            {
                Downloads = downloads.OrderBy(d => d.Name).ToList(),
                Categories = categories.OrderBy(c => c.CategoryName).ToList()
            };
            return model;
        }

        public DownloadCategory GetCategory(int id)
        {
            var directories = Directory.GetDirectories(Globals.BasePath + "\\Data\\Downloads\\");
            var categories =
                directories.Select(directory => Path.GetFullPath(directory).Split('\\').Last())
                    .Select(catDir => new DownloadCategory
                    {
                        CategoryName = catDir.Split('_')[1],
                        Id = Convert.ToInt32(catDir.Split('_')[0])
                    }).ToList();
            return categories.FirstOrDefault(category => category.Id == id);
        }
        #endregion


    }
}

