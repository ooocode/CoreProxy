using CoreProxy.Common;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using ServerWebApplication;
using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CoreProxy
{
    public class Local
    {
        public Local()
        {

        }

        private string remoteAddress;
        private int remotePort;
        private ILogger<Local> logger;

        public async Task StartAsync(ILogger<Local> logger,
            IConnectionListenerFactory listenerFactory,
                                     string localListenAddress,
                                     int localListenPort,
                                     string remoteAddress,
                                     int remotePort)
        {

            this.logger = logger;
            this.remoteAddress = remoteAddress;
            this.remotePort = remotePort;

            try
            {
                var bind = await listenerFactory.BindAsync(new IPEndPoint(IPAddress.Parse(localListenAddress), localListenPort));
                logger.LogInformation($"客户端正在监听{localListenPort}端口");

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


        /// <summary>
        /// 浏览器tcp转发
        /// </summary>
        /// <param name="client"></param>
        private async Task TcpHandlerAsync(ConnectionContext browser)
        {
            await using SocketConnect target = new SocketConnect();
            try
            {
                await target.ConnectAsync(remoteAddress, remotePort, browser);

                while (true)
                {
                    //浏览器普通接收
                    var result = await browser.Transport.Input.ReadAsync();
                    ReadOnlySequence<byte> buff = result.Buffer;
                    if (buff.IsEmpty)
                    {
                        break;
                    }

                    SequencePosition position = result.Buffer.Start;
                    if (buff.TryGet(ref position, out ReadOnlyMemory<byte> memory) && memory.Length > 0)
                    {
                        // 接收到浏览器数据
                        if (memory.Length == 3 && string.Join(",", memory.ToArray()) == "5,1,0")
                        {
                            //发5 0 回到浏览器
                            await browser.Transport.Output.WriteAsync(new byte[] { 5, 0 });
                        }
                        else
                        {
                            //发送数据到服务器
                            await target.SendAsync(memory);
                        }

                        browser.Transport.Input.AdvanceTo(buff.GetPosition(memory.Length));
                    }

                    if (result.IsCompleted || result.IsCanceled)
                    {
                        break;
                    }
                }

                await browser.Transport.Input.CompleteAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
        }
    }
}
