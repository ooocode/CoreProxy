using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ServerWebApplication
{
    public class SocketConnect : IDisposable
    {
        //TcpClient TcpClient = new TcpClient();
        private Socket socket { get; set; }
        //public Pipe pipe { get; set; }

        public Channel<byte[]> ChannelTcp { get; set; }

        public async Task ConnectAsync(IPAddress host, int port)
        {
            ChannelTcp = Channel.CreateUnbounded<byte[]>();
            //pipe = new Pipe();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(host, port);
            ThreadPool.QueueUserWorkItem(new WaitCallback(async (obj) =>
            {
                await RecvAsync();
            }));

        }


        private async Task RecvAsync()
        {
            try
            {
                byte[] buffer = new byte[8192];

                while (true)
                {
                    // 从PipeWriter至少分配512字节
                    //Memory<byte> memory = pipe.Writer.GetMemory(minimumBufferSize);

                    int bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    var result = buffer.Take(bytesRead).ToArray();
                    await ChannelTcp.Writer.WriteAsync(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RecvAsync发生错误:{ex.InnerException?.Message ?? ex.Message}");
            }

            ChannelTcp.Writer.Complete();
        }

        public async Task SendAsync(ReadOnlyMemory<byte> memory)
        {
            await socket.SendAsync(memory, SocketFlags.None);
        }

        public void Dispose()
        {
            if (socket != null)
            {
                socket.Close();
            }
        }
    }
}
