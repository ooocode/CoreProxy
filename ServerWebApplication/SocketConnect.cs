using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ServerWebApplication
{
    public class SocketConnect : IDisposable
    {
        private readonly ILogger logger;

        private Socket socket { get; set; }


        public SocketConnect(ILogger logger)
        {
            this.logger = logger;
        }

        public async Task ConnectAsync(IPAddress host, int port)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(host, port);
        }


        public async IAsyncEnumerable<byte[]> GetRecvDataAsync()
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

                yield return result;
            }
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
