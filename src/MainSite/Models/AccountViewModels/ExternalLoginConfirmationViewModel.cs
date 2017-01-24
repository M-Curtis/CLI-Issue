#region Usings

using System.ComponentModel.DataAnnotations;

#endregion

namespace MainSite.Models.AccountViewModels
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}