using System.ComponentModel.DataAnnotations;

namespace MainSite.Models
{
    public class Address
    {
        public Address() {}
        public int Id { get; set; }
        public int Cid { get; set; }
        public int Number { get; set; }
        public string Street { get; set; }
        public string County { get; set; }
        public string City { get; set; }
        [Display(Name ="Post Code")]
        public string PostCode { get; set; }
    }
}
