#region Usings

using System.ComponentModel.DataAnnotations;
using System.IO;
using MainSite.Data;
using static MainSite.Debug.UtilityClass;

#endregion

namespace MainSite.Models
{
    public class Documentation
    {
        [Key]
        public int Id { get; set; }

        public int CgId { get; set; }

        public string Title { get; set; }

        public string FileName { get; set; }

        public string Creator { get; set; }

        public Documentation(int id, int cgid,string title, string file, string document, string creator, ApplicationDbContext context)
        {
            Id = id;
            CgId = cgid;
            Title = title;
            Creator = creator;
            foreach (var x in Globals.PathInvalids)
            {
                if (file.Contains(x.ToString()))
                {
                    file = file.Replace(x, '.');
                }
            }
            if (File.Exists($@"{Globals.BasePath}\{file}"))
            {
                file = FileRecurse(file, 1);
            }
            using (var writefile = File.CreateText($@"{Globals.BasePath}\{file}"))
            {
                writefile.Write(document);
            }
            FileName = file;
        }

        public Documentation() { }
    }
    
}
