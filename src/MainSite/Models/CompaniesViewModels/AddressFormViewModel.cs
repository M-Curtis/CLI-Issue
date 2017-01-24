using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MainSite.Models.CompaniesViewModels
{
    public class AddressFormViewModel
    {
        public Company Company { get; set; }
        public List<Address> Addresses { get; set; }
    }
}
