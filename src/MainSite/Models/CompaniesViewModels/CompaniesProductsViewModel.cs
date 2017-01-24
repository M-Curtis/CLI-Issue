using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MainSite.Models.CompaniesViewModels
{
    public class CompaniesProductsViewModel
    {
        public List<CompanyViewModel> ModelCompany { get; set; }
        public List<ProductViewModel> ModelProduct { get; set; }
    }
}
