#region Usings

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

#endregion

namespace MainSite.Models.ManageViewModels
{
    public class ConfigureTwoFactorViewModel
    {
        public string SelectedProvider { get; set; }

        public ICollection<SelectListItem> Providers { get; set; }
    }
}