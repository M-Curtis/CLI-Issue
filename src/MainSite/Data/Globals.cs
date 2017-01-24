#region Usings

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MainSite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Remotion.Linq.Clauses.ResultOperators;

#endregion

namespace MainSite.Data
{
    public static class Globals
    {
        public static readonly string BasePath = AppContext.BaseDirectory;
        public static readonly char[] PathInvalids = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        public static string Notification(Notification e, string message)
        {
            string notification;
            switch (e)
            {
                default:
                    throw new Exception("Invalid Enum Expression, only use valid enums from the Notification enumerator");
                case Data.Notification.Success: //Success
                {
                    notification = $"toastr[\"success\"](\"{message}\")";
                        break;
                    }
                case Data.Notification.Error: //Error
                    {
                        notification = $"toastr[\"error\"](\"{message}\")";
                        break;
                    }
                case Data.Notification.Info: //Info
                    {
                        notification = $"toastr[\"info\"](\"{message}\")";
                        break;
                    }
                case Data.Notification.Warning: //Warning
                    {
                        notification = $"toastr[\"warning\"](\"{message}\")";
                        break;
                    }
            }
            return notification;
        }

        public static async void SendError(Exception e)
        {
           await new AuthMessageSender().SendEmailAsync("cmullett@uti400.com", "Error: Intranet", new StackTrace(e, true).GetFrames().Aggregate("", (current, frame) => current + $"Exception thrown: <br/> {frame.GetMethod()} <br/> {{File::}} {frame.GetFileName()} {frame.GetFileLineNumber()} {frame.GetFileColumnNumber()}<br/>...<br/><br/>"));
        }

        public static void CreateDirectories()
        {
            Directory.CreateDirectory(BasePath + "\\Data\\Companies\\Notes\\");
            Directory.CreateDirectory(BasePath + "\\Data\\Documentation\\");
            Directory.CreateDirectory(BasePath + "\\Data\\VPNs\\Notes\\");
            Directory.CreateDirectory(BasePath + "\\Data\\Downloads\\1_Default\\");
        }

        public static void CreateFiles()
        {
            if (!File.Exists(BasePath + "\\em.conf"))
            {
                using (var fs = new FileStream(BasePath + "\\em.conf", FileMode.Create, FileAccess.Write))
                using (var sw = new StreamWriter(fs))
                {
                    sw.WriteLine("Email: ,");
                    sw.WriteLine("Password: ");
                }
            }
                
        }

        public static RedirectToActionResult Error(int errorCode)
        {
            if (errorCode <= 0) throw new ArgumentOutOfRangeException(nameof(errorCode));
            if (ErrorExist(errorCode))
            {
                var temp = new Controllers.AccountController(null,null,null,null,null);
                temp.RedirectToAction("Index", "Error", new {id = errorCode});
            }
            throw new ArgumentOutOfRangeException($"Error code must be suppported you supplied code {errorCode}");
        }

        public static bool ErrorExist(int id)
        {
            var path = Path.Combine(BasePath, "Views\\Error\\");
            return File.Exists(Path.Combine(path, id.ToString(), ".cshtml"));
        }
    }
    public enum Notification
    {
        Success, Error, Info, Warning
    }
}
