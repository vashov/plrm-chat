using Plrm.Chat.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private AuthManager _authManager;
        private readonly IPAddress _serverAddress;
        private readonly int _serverPort;

        private TcpClient _client;

        private object _lock = new object();

        private Thread _threadReceive = null;
        private CancellationTokenSource _receiveTokenSource = default;

        public Chat(AuthManager authManager, IPAddress serverAddress, int serverPort)
        {
            _authManager = authManager;
            _serverAddress = serverAddress;
            _serverPort = serverPort;
        }

        public void Connect()
        {
            lock(_lock)
            {
                _client = new TcpClient();
                _client.Connect(_serverAddress, _serverPort);

                _receiveTokenSource = new CancellationTokenSource();
                _threadReceive = new Thread(t => ReceiveData((CancellationToken)t));
                _threadReceive.Start(_receiveTokenSource.Token);

                Console.WriteLine("Server: Connected.");
            }
        }

        public void Reconnect()
        {
            Disconnect();
            Connect();

            Console.WriteLine("Server: Reconnected.");
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
                Console.WriteLine($"Disconnect: {e}");
            }

            Console.WriteLine("Server: Disconnected.");
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
                Console.WriteLine($"Chat SendMessage: {e}");
            }
        }

        public bool? LogInToChat()
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
                Console.WriteLine($"{e}");
                return false;
            }

            while (_authManager.IsLoggedIn == null)
            {
                Console.WriteLine("Wait authorization response ... ");
                Thread.Sleep(100);
            }

            return _authManager.IsLoggedIn;
        }

        public void Dispose()
        {
            // TODO: Right implementation of Dispose
            Disconnect();
        }

        private void ReceiveData(CancellationToken token)
        {
            Console.WriteLine("Chat ReceiveData: Start receiving.");

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

                    if (_authManager.IsLoggedIn != true)
                    {
                        var json = Encoding.UTF8.GetString(receivedBytes, 0, byte_count);
                        var authResponse = JsonSerializer.Deserialize<AuthorizationResponse>(json);
                        if (authResponse.IsOk)
                        {
                            _authManager.SetStateLoginSuccess();
                            continue;
                        }

                        _authManager.SetStateLoginFailed(authResponse.Error);
                        continue;
                    }
                    Console.WriteLine(Encoding.UTF8.GetString(receivedBytes, 0, byte_count));
                }

                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Chat ReceiveData: IsCancellationRequested.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Chat ReceiveData: {e}");
            }

        }
    }
}
