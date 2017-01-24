using System.Collections.Generic;
using MainSite.Models.MachineViewModels;

namespace MainSite.Models.CompaniesViewModels
{
    public class CompanyDetailsViewModel
    {
        public Company Company { get; set; }
        public List<VpnConnectionList> VpNs { get; set; }
        public List<List<MachineProductsViewModel>> MachineProducts { get; set; }
        public List<Address> Addresses { get; set; }
    }
}
