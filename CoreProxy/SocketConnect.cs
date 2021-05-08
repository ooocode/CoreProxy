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
        ConnectionContext browser;

        public SocketConnect()
        {

        }

        public async Task ConnectAsync(string host, int port, ConnectionContext browser, string socketTargetAddress, int socketTargetPort)
        {
            this.browser = browser;
            hubConnection = new HubConnectionBuilder()
              .WithUrl($"http://{host}:{port}/chatHub", options =>
               {
                   options.AccessTokenProvider = () => Task.FromResult($"{socketTargetAddress}:{socketTargetPort}");
               })
              //.WithAutomaticReconnect()
              .ConfigureLogging(builder =>
              {
                  builder.AddConsole();
                  builder.SetMinimumLevel(LogLevel.Warning);
              })
              .Build();

            hubConnection.On("ReceiveMessage", async (byte[] data) =>
            {
                await browser.Transport.Output.WriteAsync(data);
            });

            hubConnection.Closed += HubConnection_Closed;
            await hubConnection.StartAsync();
        }

        private async Task HubConnection_Closed(Exception arg)
        {
            if (browser != null)
            {
                await browser.Transport.Input.CompleteAsync();
            }
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
