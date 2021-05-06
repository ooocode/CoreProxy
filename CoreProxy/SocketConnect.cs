using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ServerWebApplication
{
    public class SocketConnect : IAsyncDisposable
    {
        HubConnection hubConnection;

        public SocketConnect()
        {

        }

        public async Task ConnectAsync(string host, int port, ConnectionContext browser)
        {
            hubConnection = new HubConnectionBuilder()
              .WithUrl($"http://{host}:{port}/chatHub")
              .WithAutomaticReconnect()
              .ConfigureLogging(builder =>
              {
                  builder.AddConsole();
                  builder.SetMinimumLevel(LogLevel.Warning);
              })
              .Build();

            await hubConnection.StartAsync();
            hubConnection.On("ReceiveMessage", async (byte[] data) =>
            {
                await browser.Transport.Output.WriteAsync(data);
            });
        }


        public async Task SendAsync(ReadOnlyMemory<byte> memory)
        {
            await hubConnection.SendAsync("BrowserMethod", memory.ToArray());
        }


        public async ValueTask DisposeAsync()
        {
            await hubConnection.StopAsync();
            await hubConnection.DisposeAsync();
        }
    }
}
