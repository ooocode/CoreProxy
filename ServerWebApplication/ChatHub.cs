using DnsClient;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServerWebApplication
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> logger;
        private readonly ILookupClient lookupClient;
        private readonly IConnectionFactory connectionFactory;

        public ChatHub(ILogger<ChatHub> logger, ILookupClient lookupClient, IConnectionFactory connectionFactory)
        {
            this.logger = logger;
            this.lookupClient = lookupClient;
            this.connectionFactory = connectionFactory;
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
                    var target = new SocketConnect(logger, connectionFactory);
                    await target.ConnectAsync(record.Address, int.Parse(arr[1]));
                    logger.LogTrace($"成功连接到：{arr[0]} {record.Address}:{arr[1]}");
                    Tagets.TryAdd(Context.ConnectionId, target);
                }
            }

            await base.OnConnectedAsync();
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
        /// 获取目标服务器数据
        /// </summary>
        /// <returns></returns>
        public async IAsyncEnumerable<byte[]> GetTargetServerData()
        {
            if (Tagets.TryGetValue(Context.ConnectionId, out var target))
            {
                //server返回确认消息
                byte[] sendData = new byte[] { 0x05, 0x00, 0x00, 0x01, 0x7f, 0x00, 0x00, 0x01, 0x1f, 0x40 };
                yield return sendData;

                await foreach (var data in target.GetRecvDataAsync())
                {
                    ////发往浏览器
                    yield return data;
                }
            }
        }


        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (Tagets.TryRemove(Context.ConnectionId, out SocketConnect s))
            {
                logger.LogInformation("断开signalR连接：" + s.IPEndPoint.ToString());
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}