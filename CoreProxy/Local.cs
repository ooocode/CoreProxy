using CoreProxy.Common;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
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

    [DisallowConcurrentExecution]
    public class Local : Quartz.IJob
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
            if (string.IsNullOrEmpty(remoteAddress))
            {
                throw new Exception(nameof(remoteAddress) + " is null");
            }
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
                await target.ConnectAsync(remoteAddress, remotePort, System.Text.Encoding.UTF8.GetString(socket5Result.Address), socket5Result.Port);
                logger.LogInformation($"连接到{System.Text.Encoding.UTF8.GetString(socket5Result.Address)}:{socket5Result.Port}");

                Task taskRecvBrowser = Task.Run(async () =>
                {
                    //获取浏览器数据
                    await foreach (var browserData in GetRecvDataAsync(browser))
                    {
                        //发送数据到服务器
                        await target.SendAsync(browserData);
                    }
                });

                Task taskRecvServer = Task.Run(async () =>
                {
                    //接收服务器数据
                    await foreach (var browserData in target.hubConnection.StreamAsync<byte[]>("GetTargetServerData"))
                    {
                        //发送数据到浏览器
                        await browser.Transport.Output.WriteAsync(browserData);
                    }
                });

                await Task.WhenAll(taskRecvBrowser, taskRecvServer);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"处理TcpHandlerAsync出现错误：{ex.InnerException?.Message ?? ex.Message}");
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

            await connectionContext.Transport.Input.CompleteAsync();
        }


        public async Task Execute(IJobExecutionContext context)
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
                        try
                        {
                            await TcpHandlerAsync(browser);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex.InnerException?.Message ?? ex.Message);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
