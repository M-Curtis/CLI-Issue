using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MainSite.Data;

namespace MainSite.Models.DownloadViewModels
{
    public class DownloadCategory
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        /// <summary>
        /// Creates a new Category with a directory.
        /// </summary>
        /// <param name="name"></param>
        public DownloadCategory(string name)
        {
            Id = LastId() + 1;
            CategoryName = name;
            Directory.CreateDirectory(Globals.BasePath + "\\Data\\Downloads\\" + Id + "_" + name);
        }
        public DownloadCategory() { }

        private int LastId()
        {
            int i = 0;
            foreach (var directory in Directory.GetDirectories(Globals.BasePath + "\\Data\\Downloads\\"))
            {
                var x = Convert.ToInt32(directory.Split('\\').Last().Split('_')[0]);
                if (x > i)
                {
                    i = x;
                }
            }
            return i;
        }
    }
}
