#region Usings

using System.ComponentModel.DataAnnotations;

#endregion

namespace MainSite.Models.ManageViewModels
{
    public class AddPhoneNumberViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }
    }
}