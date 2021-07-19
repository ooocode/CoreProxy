using DnsClient;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ServerWebApplication;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SignalRChat.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> logger;
        private readonly ILookupClient lookupClient;

        public ChatHub(ILogger<ChatHub> logger, ILookupClient lookupClient)
        {
            this.logger = logger;
            this.lookupClient = lookupClient;
        }


        private static System.Collections.Concurrent.ConcurrentDictionary<string, SocketConnect> Tagets = new System.Collections.Concurrent.ConcurrentDictionary<string, SocketConnect>();


        public override async Task OnConnectedAsync()
        {
            if (Context.GetHttpContext().Request.Headers.TryGetValue("Authorization", out var value))
            {
                var arr = value.ToString().Replace("Bearer ", string.Empty).Split(':');
                var record = (await lookupClient.QueryAsync(arr[0], QueryType.A)).Answers.ARecords().FirstOrDefault();
                if (record != null)
                {
                    var target = new SocketConnect(logger);
                    await target.ConnectAsync(record.Address, int.Parse(arr[1]));
                    logger.LogInformation($"成功连接到：{arr[0]} {record.Address}:{arr[1]}");

                    ProcessTargetServer(this.Clients.Caller, target);
                    Tagets.TryAdd(this.Context.ConnectionId, target);

                    //server返回确认消息
                    byte[] sendData = new byte[] { 0x05, 0x00, 0x00, 0x01, 0x7f, 0x00, 0x00, 0x01, 0x1f, 0x40 };
                    await Clients.Caller.SendAsync("ReceiveMessage", sendData);
                }
            }

            await base.OnConnectedAsync();
        }


        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (Tagets.TryRemove(Context.ConnectionId, out var s))
            {
                s.Dispose();
            }

            return base.OnDisconnectedAsync(exception);
        }

        //浏览器发数据过来
        public async Task BrowserMethod(byte[] data)
        {
            if (Tagets.TryGetValue(Context.ConnectionId, out var target))
            {
                //发送数据到目标服务器
                await target.SendAsync(data);
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
                    await foreach (var data in target.GetRecvDataAsync())
                    {
                        ////发往浏览器
                        await browser.SendAsync("ReceiveMessage", data);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("ProcessTargetServer:" + ex.Message);
                }
            }).Start();
        }
    }
}