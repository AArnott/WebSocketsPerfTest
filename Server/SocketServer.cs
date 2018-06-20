using System.Threading.Tasks;

namespace Server
{
    public class SocketServer : ISocketServer
    {
        public Task<string> HiAsync(string name) => Task.FromResult($"Hi, {name}!");
    }
}
