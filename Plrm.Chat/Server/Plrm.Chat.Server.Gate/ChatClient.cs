using Plrm.Chat.Server.Gate.Repositories.Users.Models;
using System.Net.Sockets;

namespace Plrm.Chat.Server.Gate
{
    class ChatClient
    {
        public long Id { get; }
        public TcpClient Client { get; }
        public User User { get; private set; }

        public ChatClient(long id, TcpClient client)
        {
            Id = id;
            Client = client;
        }

        public void Authorize(User user)
        {
            User = user;
        }
    }
}
