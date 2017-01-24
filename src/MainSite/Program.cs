#region Usings

using System.IO;
using MainSite.Data;
using Microsoft.AspNetCore.Hosting;

#endregion

namespace MainSite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Globals.CreateDirectories();
            Globals.CreateFiles();
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();
            host.Run();
        }
    }
}