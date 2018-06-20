using System.Threading.Tasks;

namespace Server
{
    public interface ISocketServer
    {
        Task<string> HiAsync(string name);
    }
}
