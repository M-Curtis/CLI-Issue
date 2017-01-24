using System.Collections.Generic;

namespace MainSite.Models.MachineViewModels
{
    public class MachineMachineCredentialsViewModel
    {
        public Machine Machine { get; set; }
        public List<MachineCredentials> Credentials { get; set; }
    }
}
