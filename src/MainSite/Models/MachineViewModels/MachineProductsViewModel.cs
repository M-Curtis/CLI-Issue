using System;

namespace MainSite.Models.MachineViewModels
{
    public class MachineProductsViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string Version { get; set; }
        public string LicenseKey { get; set; }
        public DateTime? Expires { get; set; }

        public MachineProductsViewModel() { }
    }
}