#region Usings

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace MainSite.Models
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class Contact
    {
        public Contact()
        {
        }

        public Contact(int id, int cid, string firstName, string lastName, string email, string phone, string mobile)
        {
            Id = id;
            Cid = cid;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Phone = phone;
            MobilePhone = mobile;
        }

        [Key]
        public int Id { get; set; }

        [Display(Name = "Company")]
        public int Cid { get; set; }

        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Display(Name = "Surname")]
        public string LastName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        [Display(Name = "Mobile")]
        public string MobilePhone { get; set; }
    }
}