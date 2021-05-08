using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ServerWebApplication
{
    public class SocketConnect : IDisposable
    {

        //TcpClient TcpClient = new TcpClient();
        private Socket socket { get; set; }
        public Pipe pipe { get; set; }

        public NetworkStream Stream { get; set; }

        public async Task ConnectAsync(IPAddress host, int port)
        {
            //await TcpClient.ConnectAsync(host, port);
            //Stream = TcpClient.GetStream();
            pipe = new Pipe();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(host, port);
            RecvAsync();
        }


        private async Task RecvAsync()
        {
            const int minimumBufferSize = 512;

            while (true)
            {
                // 从PipeWriter至少分配512字节
                Memory<byte> memory = pipe.Writer.GetMemory(minimumBufferSize);

                int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
                if (bytesRead == 0)
                {
                    break;
                }

                // 告诉PipeWriter从套接字读取了多少
                pipe.Writer.Advance(bytesRead);

                // 标记数据可用，让PipeReader读取
                FlushResult result = await pipe.Writer.FlushAsync();

                if (result.IsCompleted)
                {
                    break;
                }
            }

            // 告诉PipeReader没有更多的数据
            pipe.Writer.Complete();
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
