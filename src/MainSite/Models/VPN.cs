using System.ComponentModel.DataAnnotations;

namespace MainSite.Models
{
    public class Vpn
    {
        [Key]
        public int Id { get; set; }
        public int Cid { get; set; }
        public string Address { get; set; }
        public string Type { get; set; }

        public Vpn() { }
    }
}