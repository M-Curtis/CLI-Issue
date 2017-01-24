#region Usings

using System.IO;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;

#endregion

namespace MainSite.Services
{
    // This class is used by the application to send Email and SMS
    // when you turn on two-factor authentication in ASP.NET Identity.
    // For more details see this link http://go.microsoft.com/fwlink/?LinkID=532713


    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        private readonly EmailOptions _sender = new EmailOptions(EmailOptions.GetEmail(), EmailOptions.GetPassword());

        public async Task<Task> SendEmailAsync(string email, string subject, string message)
        {
            // Plug in your email service here to send an email.
            var myMessage = new MimeMessage
            {
                To = {new MailboxAddress(email.Split('@')[0], email)},
                From = {new MailboxAddress("Intranet", _sender.Email)},
                Subject = subject,
                Body = new TextPart("html")
                {
                    Text = message
                }
            };
            var client = new SmtpClient {ServerCertificateValidationCallback = (s, c, h, e) => true};
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            client.Connect("mail.utilities400.com", 25, false);
            client.Authenticate(_sender.Email, _sender.Password);
            await client.SendAsync(myMessage);
            return null;
        }

        public Task SendSmsAsync(string number, string message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }

    public class EmailOptions
    {
        public string Email { get; }
        public string Password { get; }

        public EmailOptions(string email, string password)
        {
            Email = email;
            Password = password;
        }

        public static string GetEmail()
        {
            using (var fs = new FileStream(@"C:\Web\Intranet\em.conf", FileMode.Open, FileAccess.Read))
            using (var sr = new StreamReader(fs))
            {
                return sr.ReadToEnd().Split(',')[0].Split(':')[1].Trim();
            }
        }

        public static string GetPassword()
        {
            using (var fs = new FileStream(@"C:\Web\Intranet\em.conf", FileMode.Open, FileAccess.Read))
            using (var sr = new StreamReader(fs))
            {
                return sr.ReadToEnd().Split(',')[1].Split(':')[1].Trim();
            }
        }
    }
}