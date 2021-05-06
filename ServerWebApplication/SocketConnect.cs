using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ServerWebApplication
{
    public class SocketConnect : IDisposable
    {

        TcpClient TcpClient = new TcpClient();


        public void Close()
        {
            TcpClient.Dispose();
        }


        public NetworkStream Stream { get; set; }

        public async Task ConnectAsync(string host, int port)
        {
            await TcpClient.ConnectAsync(host, port);
            Stream = TcpClient.GetStream();
        }


        public async Task SendAsync(ReadOnlyMemory<byte> memory)
        {
            await Stream.WriteAsync(memory);
        }

        public void Dispose()
        {
            this.Close();
        }
    }
}
