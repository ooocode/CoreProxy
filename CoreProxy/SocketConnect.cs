using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CoreProxy
{
    public class SocketConnect : IAsyncDisposable
    {
        public HubConnection hubConnection;

        public async Task ConnectAsync(string host, int port, string socketTargetAddress, int socketTargetPort)
        {
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
            await hubConnection.StartAsync();
        }

        public async Task SendAsync(ReadOnlyMemory<byte> memory)
        {
            await hubConnection.SendAsync("BrowserMethod", memory.ToArray());
        }


        public async ValueTask DisposeAsync()
        {
            if (hubConnection != null)
            {
                await hubConnection.StopAsync();
                await hubConnection.DisposeAsync();
            }
        }
    }
}
