using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CoreProxy
{
    public class Program
    {
        static void ShowInfomation()
        {
            Console.WriteLine("可以设置开机启动项");
            Console.WriteLine("     C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\StartUp");


            Console.WriteLine("运行IE代理--pac代理     http://127.0.0.1:520/pac.txt");
            Console.WriteLine("          --全局代理    http://127.0.0.1:520/global.txt");
        }


        static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
                registry.SetValue("AutoConfigURL", "http://127.0.0.1:520/pac.txt");
            }

            ShowInfomation();
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://localhost:520");
                    webBuilder.UseStartup<Startup>();
                }).Build().Run();
        }
    }
}
