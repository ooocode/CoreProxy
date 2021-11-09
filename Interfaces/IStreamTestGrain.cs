
using Orleans.Streams;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IStreamTestGrain : Orleans.IGrainWithStringKey
    {
        Task<bool> SendMessageAsync(byte[] payload);

        Task<StreamSubscriptionHandle<byte[]>> SubscribeAsync(IChat chat);

        Task<bool> SetRemoteAddressAndPortAsync(string remoteAddress,int port);
        Task UnsubscribeAsync();
    }
}