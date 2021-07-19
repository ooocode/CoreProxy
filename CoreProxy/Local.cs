using CoreProxy.Common;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServerWebApplication;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CoreProxy
{
    public record Message(long Bytes);

    public class Local : BackgroundService
    {
        private string remoteAddress;
        private int remotePort;
        private ILogger<Local> logger;

        private readonly IConnectionListenerFactory connectionListenerFactory;

        public Local(IConnectionListenerFactory connectionListenerFactory,
            IConfiguration configuration,
            ILogger<Local> logger)
        {
            this.connectionListenerFactory = connectionListenerFactory;
            this.logger = logger;
            remoteAddress = configuration["RemoteConnectAddress"];
            if (!int.TryParse(configuration["RemoteConnectPort"], out int remotePort))
            {
                remotePort = 2020;
            }

            this.remotePort = remotePort;
        }


        /// <summary>
        /// 浏览器tcp转发
        /// </summary>
        /// <param name="client"></param>
        private async Task TcpHandlerAsync(ConnectionContext browser)
        {
            try
            {
                //browser-->5,1,0
                var firstPack = (await browser.Transport.Input.ReadAsync()).Buffer;
                if (string.Join(",", firstPack.ToArray()) != "5,1,0")
                {
                    throw new Exception("proxy handshake faild");
                }

                browser.Transport.Input.AdvanceTo(firstPack.GetPosition(firstPack.Length));

                //server-->5,0
                //发5 0 回到浏览器
                await browser.Transport.Output.WriteAsync(new byte[] { 5, 0 });

                //browser-->address port
                var secondPack = (await browser.Transport.Input.ReadAsync()).Buffer;
                if (!Socket5Utility.TryParse(secondPack.ToArray(), out var socket5Result))
                {
                    throw new Exception("parse socket5 proxy infomation faild");
                }

                browser.Transport.Input.AdvanceTo(secondPack.GetPosition(secondPack.Length));

                await using SocketConnect target = new SocketConnect();
                await target.ConnectAsync(remoteAddress, remotePort, browser, System.Text.Encoding.UTF8.GetString(socket5Result.Address), socket5Result.Port);


                await foreach (var browserData in GetRecvDataAsync(browser))
                {
                    //发送数据到服务器
                    await target.SendAsync(browserData);
                }

                await browser.Transport.Input.CompleteAsync();
            }
            catch (Exception ex)
            {
                logger.LogError($"处理TcpHandlerAsync出现错误：{ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async IAsyncEnumerable<ReadOnlyMemory<byte>> GetRecvDataAsync(ConnectionContext connectionContext)
        {
            while (true)
            {
                //浏览器普通接收
                var result = await connectionContext.Transport.Input.ReadAsync();
                ReadOnlySequence<byte> buff = result.Buffer;
                if (buff.IsEmpty)
                {
                    break;
                }

                SequencePosition position = result.Buffer.Start;
                while (buff.TryGet(ref position, out ReadOnlyMemory<byte> memory) && memory.Length > 0)
                {
                    yield return memory;
                }

                connectionContext.Transport.Input.AdvanceTo(buff.End);

                if (result.IsCompleted || result.IsCanceled)
                {
                    break;
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var bind = await connectionListenerFactory.BindAsync(new IPEndPoint(IPAddress.Loopback, 1080));
                logger.LogInformation($"客户端正在监听1080端口");

                while (true)
                {
                    ConnectionContext browser = await bind.AcceptAsync();
                    ThreadPool.QueueUserWorkItem(new WaitCallback(async (obj) =>
                    {
                        await TcpHandlerAsync(browser);
                    }));
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex.Message);
            }
        }
    }
}
