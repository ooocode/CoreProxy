using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace ServerWebApplication
{
    public class SocketConnect
    {
        private readonly ILogger logger;
        private readonly IConnectionFactory connectionFactory;
        private ConnectionContext socket;
        public IPEndPoint IPEndPoint;


        public SocketConnect(ILogger logger, IConnectionFactory connectionFactory)
        {
            this.logger = logger;
            this.connectionFactory = connectionFactory;
        }

        public async Task ConnectAsync(IPAddress host, int port)
        {
            IPEndPoint = new IPEndPoint(host, port);
            socket = await connectionFactory.ConnectAsync(IPEndPoint);
        }


        public async IAsyncEnumerable<byte[]> GetRecvDataAsync()
        {
            while (true)
            {
                // 从PipeWriter至少分配512字节
                //Memory<byte> memory = pipe.Writer.GetMemory(minimumBufferSize);

                var result = await socket.Transport.Input.ReadAsync();
                ReadOnlySequence<byte> buff = result.Buffer;
                if (buff.IsEmpty)
                {
                    //logger.LogInformation("GetRecvDataAsync IsEmpty");
                    break;
                }

                SequencePosition position = result.Buffer.Start;
                while (buff.TryGet(ref position, out ReadOnlyMemory<byte> memory) && memory.Length > 0)
                {
                    yield return memory.ToArray();
                }

                socket.Transport.Input.AdvanceTo(buff.End);

                if (result.IsCompleted)
                {
                    //logger.LogInformation("GetRecvDataAsync IsCompleted");
                    break;
                }

                if (result.IsCanceled)
                {
                    //logger.LogInformation("GetRecvDataAsync IsCanceled");
                    break;
                }
            }

            await socket.Transport.Input.CompleteAsync();
        }

        public async Task SendAsync(ReadOnlyMemory<byte> memory)
        {
            await socket.Transport.Output.WriteAsync(memory);
        }
    }
}
