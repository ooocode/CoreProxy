using CoreProxy.Common;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ServerWebApplication;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRChat.Hubs
{
    public class ChatHub : Hub
    {
        public ChatHub(ILogger<ChatHub> logger)
        {
            this.logger = logger;
        }


        static System.Collections.Concurrent.ConcurrentDictionary<string, SocketConnect> Tagets = new System.Collections.Concurrent.ConcurrentDictionary<string, SocketConnect>();
        private readonly ILogger<ChatHub> logger;

        public override Task OnConnectedAsync()
        {
            Tagets.TryAdd(this.Context.ConnectionId, new SocketConnect());

            return base.OnConnectedAsync();
        }


        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (Tagets.TryRemove(Context.ConnectionId, out var s))
            {
                s.Dispose();
            }

            return base.OnDisconnectedAsync(exception);
        }


        public async Task BrowserMethod(byte[] data)
        {
            if (Tagets.TryGetValue(Context.ConnectionId, out var target))
            {
                Socket5Info socket5Info = new Socket5Info();
                if (socket5Info.TryParse(data))
                {
                    var address = System.Text.Encoding.UTF8.GetString(socket5Info.Address);
                    logger.LogInformation("开始连接到：" + address + ":" + socket5Info.Port);
                    await target.ConnectAsync(address, socket5Info.Port);

                    byte[] sendData = new byte[] { 0x05, 0x00, 0x00, 0x01, 0x7f, 0x00, 0x00, 0x01, 0x1f, 0x40 };

                    //发送确认到浏览器
                    await this.Clients.Caller.SendAsync("ReceiveMessage", sendData);

                    ProcessTargetServer(this.Clients.Caller, target);
                }
                else
                {
                    //发送数据到目标服务器
                    await target.SendAsync(data);
                }
            }
        }

        /// <summary>
        /// 监听网站目标服务器
        /// </summary>
        public void ProcessTargetServer(IClientProxy browser, SocketConnect target)
        {
            new Task(async () =>
            {
                try
                {
                    var memeory = new byte[4096];
                    while (true)
                    {
                        int readResult = await target.Stream.ReadAsync(memeory);
                        if (readResult == 0)
                        {
                            break;
                        }

                        var data = memeory.Take(readResult);

                        //发往浏览器
                        await browser.SendAsync("ReceiveMessage", data.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ProcessTargetServer:" + ex.Message);
                }
            }).Start();
        }
    }
}