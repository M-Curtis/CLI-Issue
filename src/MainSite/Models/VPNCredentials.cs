using System.ComponentModel.DataAnnotations;

namespace MainSite.Models
{
    public class VpnCredentials
    {
        [Key]
        public int Id { get; set; }
        public int VpnId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        
        public VpnCredentials() { }
    }
}