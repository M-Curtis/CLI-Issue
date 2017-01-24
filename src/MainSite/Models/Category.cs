#region Usings

using System.ComponentModel.DataAnnotations;
using static MainSite.Data.Globals;
using static MainSite.Debug.UtilityClass;

#endregion

namespace MainSite.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [RegularExpression("^[A-Za-z0-9_ ]{1,25}$", ErrorMessage = "Category Name must be between 1 and 25 characters and can only contains letters, numbers, underscores and spaces")]
        public string CategoryName { get; set; }

        public string Directory { get; set; }

        public Category(int id, string name, string dir)
        {
            Id = id;
            CategoryName = name;
            foreach (var x in PathInvalids)
            {
                if (dir.Contains(x.ToString()))
                {
                    dir = dir.Replace(x, '.');
                }
            }
            if (System.IO.Directory.Exists($@"{BasePath}\{dir}"))
            {
                dir = DirRecurse(dir, 1);
            }
            Directory = dir;
        }
        public Category() { }
    }
}
