#region Usings

using System.ComponentModel.DataAnnotations;

#endregion

namespace MainSite.Models
{
    public class Company
    {
        [Key]
        public int Id { get; set; }
        
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        public string Website { get; set; }

    }
}