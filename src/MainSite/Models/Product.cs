#region Usings

using System.ComponentModel.DataAnnotations;

#endregion

namespace MainSite.Models
{
    public class Product
    {
        public Product(string productName, int id)
        {
            ProductName = productName;
            Id = id;
        }

        public Product()
        {
        }

        [Key]
        public int Id { get; set; }

        [Display(Name = "Product")]
        public string ProductName { get; set; }
    }
}