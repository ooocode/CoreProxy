using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
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

        private static async System.Collections.Generic.IAsyncEnumerable<int> GetVs()
        {
            int index = 0;
            while (true)
            {
                await Task.Delay(2000);
                Console.WriteLine(DateTime.Now.ToString());
                yield return index++;
            }
        }

        static async Task Main(string[] args)
        {
            //new Task(async () =>
            //{
            //    await foreach (var item in GetVs())
            //    {
            //        Console.WriteLine(item);
            //        await Task.Delay(3000);
            //    }

            //}).Start();

            //await Task.Delay(-1);
            ShowInfomation();

            WebHost.CreateDefaultBuilder()
                .UseUrls("http://localhost:520")
                .UseStartup<Startup>()
                .Build().Run();
        }
    }
}
