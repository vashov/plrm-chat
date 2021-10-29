using Plrm.Chat.Server.Gate.Repositories.Users.Models;
using Plrm.Chat.Shared.Models;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

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

        public void SendAuthorizeResponseSuccess()
        {
            NetworkStream stream = Client.GetStream();

            var authResponse = new AuthorizationResponse
            {
                IsOk = true,
                Error = null
            };

            var json = JsonSerializer.Serialize(authResponse);
            byte[] response = Encoding.UTF8.GetBytes(json);
            stream.Write(response);
        }

        public void SendAuthorizeResponseError(string error)
        {
            NetworkStream stream = Client.GetStream();

            var authResponse = new AuthorizationResponse
            {
                IsOk = false,
                Error = error
            };

            var json = JsonSerializer.Serialize(authResponse);
            byte[] response = Encoding.UTF8.GetBytes(json);
            stream.Write(response);
        }
    }
}
