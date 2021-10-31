using Plrm.Chat.Server.Gate.Infrastructure;
using Plrm.Chat.Server.Gate.Repositories.Users.Models;
using Plrm.Chat.Shared.Models;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Plrm.Chat.Server.Gate
{
    class ChatClient
    {
        public long Id { get; }
        private TcpClient _client { get; }
        public User User { get; private set; }
        public bool IsAuthorized => User != null;

        public ChatClient(long id, TcpClient client)
        {
            Id = id;
            _client = client;
        }

        public void Authorize(User user)
        {
            User = user;
        }

        public async Task SendAuthorizeResponseSuccess()
        {
            NetworkStream stream = _client.GetStream();

            var authResponse = new AuthorizationResponse
            {
                IsOk = true,
                Error = null
            };

            var json = JsonSerializer.Serialize(authResponse);
            byte[] response = Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(response);
        }

        public async Task SendAuthorizeResponseError(string error)
        {
            NetworkStream stream = _client.GetStream();

            var authResponse = new AuthorizationResponse
            {
                IsOk = false,
                Error = error
            };

            var json = JsonSerializer.Serialize(authResponse);
            byte[] response = Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(response);
        }

        public async Task SendMessage(ChatMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            byte[] data = Encoding.UTF8.GetBytes(json);

            NetworkStream stream = _client.GetStream();
            await stream.WriteAsync(data);
        }

        public async Task<OperationResult<byte[]>> ReadMessage()
        {
            NetworkStream stream = _client.GetStream();
            byte[] buffer = new byte[1024];

            int byteCount;

            try
            {
                byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            }
            catch (System.IO.IOException e)
            {
                return OperationResult<byte[]>.Error($"Client {Id}: {e}");
            }
            catch (Exception e)
            {
                return OperationResult<byte[]>.Error($"Client {Id}: {e}");
            }

            if (byteCount == 0)
            {
                return OperationResult<byte[]>.Error($"Client {Id}: Empty message");
            }

            byte[] content = buffer.Take(byteCount).ToArray();
            return OperationResult<byte[]>.Ok(content);
        }

        public async Task<OperationResult<UserCredentials>> ReadAuthenticationMessage()
        {
            NetworkStream stream = _client.GetStream();
            byte[] buffer = new byte[1024];

            int byteCount;

            try
            {
                byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            }
            catch (System.IO.IOException e)
            {
                return OperationResult<UserCredentials>.Error($"Client {Id}: {e}");
            }
            catch (Exception e)
            {
                return OperationResult<UserCredentials>.Error($"Client {Id}: {e}");
            }

            if (byteCount == 0)
            {
                return OperationResult<UserCredentials>.Error($"Client {Id}: User credentials data is empty.");
            }

            var content = buffer.Take(byteCount).ToArray();
            string data = Encoding.UTF8.GetString(content);

            UserCredentials userCredentials;
            try
            {
                userCredentials = JsonSerializer.Deserialize<UserCredentials>(data);
            }
            catch (Exception e)
            {
                return OperationResult<UserCredentials>.Error($"Client {Id} UserCredentials Deserialize: {e}");
                //return OperationResult<(User, bool)>.Error("Invalid user credentials data.");
            }

            return OperationResult<UserCredentials>.Ok(userCredentials);
        }

        public void Disconnect()
        {
            _client.Client.Shutdown(SocketShutdown.Both);
            _client.Close();
            User = null;
        }
    }
}
