using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MainSite.Models.CompaniesViewModels
{
    public class VpnConnectionList
    {
        public Vpn Vpn { get; set; }
        public string VpnNotes { get; set; }
        public List<VpnCredentials> CredList { get; set; }
    }
}
