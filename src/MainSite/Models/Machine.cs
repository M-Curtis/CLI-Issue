using System.ComponentModel.DataAnnotations;

namespace MainSite.Models
{
    public class Machine
    {
        [Key]
        public int Id { get; set; }
        [Display(Name = "Model Number")]
        public string ModelNumber { get; set; }
        public string Type { get; set; }
        public int Cid { get; set; }
        public string SerialKey { get; set; }
        public string HostName { get; set; }

        public Machine() { }

    }
}
