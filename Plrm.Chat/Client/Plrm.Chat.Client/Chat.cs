using Microsoft.Extensions.Logging;
using Plrm.Chat.Shared.Models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Plrm.Chat.Client
{
    class Chat : IDisposable
    {
        private readonly ILogger<Chat> _logger;
        private AuthManager _authManager;
        private readonly IPAddress _serverAddress;
        private readonly int _serverPort;

        private TcpClient _client;

        private object _lock = new object();

        private CancellationTokenSource _receiveTokenSource = default;

        public Chat(ILogger<Chat> logger, AuthManager authManager, IPAddress serverAddress, int serverPort)
        {
            _logger = logger;
            _authManager = authManager;
            _serverAddress = serverAddress;
            _serverPort = serverPort;
        }

        public bool Connect()
        {
            lock(_lock)
            {
                try
                {
                    _receiveTokenSource = new CancellationTokenSource();

                    _client = new TcpClient();
                    _client.Connect(_serverAddress, _serverPort);

                    Task.Run(() => ReceiveData(_receiveTokenSource.Token), _receiveTokenSource.Token);

                    UIOutput.WriteLineSystem("Server: Connected.");
                    return true;
                }
                catch(Exception e)
                {
                    _logger.LogInformation($"{e}");
                    return false;
                }
            }
        }

        public void Reconnect()
        {
            Disconnect();
            Connect();

            UIOutput.WriteLineSystem("Server: Reconnected.");
        }

        public void Disconnect()
        {
            try
            {
                lock (_lock)
                {
                    _receiveTokenSource.Cancel();

                    //_client.Connected
                    _client?.Client.Shutdown(SocketShutdown.Send);
                    _client?.Close();
                    _client = null;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Disconnect: {e}");
            }

            UIOutput.WriteLineSystem("Server: Disconnected.");
        }

        public void SendMessage(string message)
        {
            var ns = _client.GetStream();

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                ns.Write(buffer, 0, buffer.Length);

            }
            catch (Exception e)
            {
                _logger.LogWarning($"Chat SendMessage: {e}");
            }
        }

        public async Task<bool?> LogInToChat()
        {
            NetworkStream stream = _client.GetStream();

            var userCredentials = new UserCredentials
            {
                Login = _authManager.Login,
                Password = _authManager.Password
            };

            var json = JsonSerializer.Serialize(userCredentials);
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(json);
                stream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Chat LogInToChat: {e}");
                return false;
            }

            while (_authManager.IsLoggedIn == null)
            {
                UIOutput.WriteLineSystem("Wait authorization response ... ");
                await Task.Delay(100);
            }

            return _authManager.IsLoggedIn;
        }

        public void Dispose()
        {
            Disconnect();
        }

        private void ReceiveData(CancellationToken token)
        {
            UIOutput.WriteLineSystem("Chat ReceiveData: Start receiving.");

            try
            {
                NetworkStream ns;
                lock (_lock)
                {
                    ns = _client.GetStream();
                }
               
                byte[] receivedBytes = new byte[1024];
                int byte_count;

                while (!token.IsCancellationRequested && _client.Connected && ns.CanRead)
                {
                    if (!((byte_count = ns.Read(receivedBytes, 0, receivedBytes.Length)) > 0))
                    {
                        return;
                    }

                    string json = Encoding.UTF8.GetString(receivedBytes, 0, byte_count);
                    if (_authManager.IsLoggedIn != true)
                    {
                        var authResponse = JsonSerializer.Deserialize<AuthorizationResponse>(json);
                        if (authResponse.IsOk)
                        {
                            _authManager.SetStateLoginSuccess();
                            continue;
                        }

                        _authManager.SetStateLoginFailed(authResponse.Error);
                        continue;
                    }

                    ChatMessage message = JsonSerializer.Deserialize<ChatMessage>(json);
                    string messageContent = Encoding.UTF8.GetString(message.Content);
                    UIOutput.WriteLineChatMessage(message.UserLogin, messageContent);
                }

                if (token.IsCancellationRequested)
                {
                    UIOutput.WriteLineSystem("Chat ReceiveData: IsCancellationRequested.");
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Chat ReceiveData: {e}");
            }

        }
    }
}
