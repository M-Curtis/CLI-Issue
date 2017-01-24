using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MainSite.Models.DownloadViewModels
{
    public class DownloadViewModel
    {
        public List<DownloadCategory> Categories { get; set; }
        public List<Download> Downloads { get; set; }
    }
}
