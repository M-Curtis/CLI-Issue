#region Usings

using System.ComponentModel.DataAnnotations;
using System;

#endregion

namespace MainSite.Models
{
    public class ProductLink
    {
        [Key]
        public int Id { get; set; }
        public int Cid { get; set; }
        public int? Pid { get; set; }
        public int? Mid { get; set; }
        public string Version { get; set; }
        public string LicenseKey { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Other { get; set; }


        public ProductLink() { }
    }
}