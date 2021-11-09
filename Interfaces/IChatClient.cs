using System.Collections.Generic;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IChatClient
    {
        Task SendAsync(byte[] message);

        IAsyncEnumerable<byte> ReceiveMessageAsync();
    }
}
