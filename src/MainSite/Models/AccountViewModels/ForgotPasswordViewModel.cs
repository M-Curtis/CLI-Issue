#region Usings

using System.ComponentModel.DataAnnotations;

#endregion

namespace MainSite.Models.AccountViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string UserName { get; set; }
    }
}