#region Usings

using System.Threading.Tasks;

#endregion

namespace MainSite.Services
{
    public interface IEmailSender
    {
        Task<Task> SendEmailAsync(string email, string subject, string message);
    }
}