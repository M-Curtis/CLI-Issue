#region Usings

using System.Threading.Tasks;

#endregion

namespace MainSite.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}