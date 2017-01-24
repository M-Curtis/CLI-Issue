using System.ComponentModel.DataAnnotations;

namespace MainSite.Models
{
    public class MachineCredentials
    {
        [Key]
        public int Id { get; set; }
        public int Mid { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        
        public MachineCredentials() { }
    }
}
