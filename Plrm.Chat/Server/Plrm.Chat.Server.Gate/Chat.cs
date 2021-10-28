using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Plrm.Chat.Server.Gate
{
    class Chat
    {

        private readonly ILogger<Chat> _logger;
        private readonly IPAddress _address;
        private readonly int _port;

        private TcpListener _listener;

        private readonly object _lock = new object();
        private readonly Dictionary<int, TcpClient> _listClients = new Dictionary<int, TcpClient>();

        public Chat(ILogger<Chat> logger, IPAddress address, int port)
        {
            _logger = logger;
            _address = address;
            _port = port;
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, 5000);
            _listener.Start();

            int count = 1;

            while (true)
            {
                TcpClient client = _listener.AcceptTcpClient();

                lock (_lock)
                {
                    _listClients.Add(count, client);
                }

                _logger.LogInformation($"User {count} connected");

                var thread = new Thread(HandleClients);
                thread.Start(count);
                count++;
            }
        }

        private void HandleClients(object o)
        {
            int id = (int)o;
            TcpClient client;

            lock (_lock)
            {
                client = _listClients[id];
            }

            while (true)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];

                int byte_count = 0;

                try
                {
                    byte_count = stream.Read(buffer, 0, buffer.Length);
                }
                catch (System.IO.IOException e)
                {
                    _logger.LogInformation($"Client {id}: {e}");
                }
                catch (Exception e)
                {
                    _logger.LogWarning($"Client {id}: {e}");
                }

                if (byte_count == 0)
                {
                    break;
                }

                string data = Encoding.UTF8.GetString(buffer, 0, byte_count);
                Broadcast(data);

                _logger.LogTrace(data);
            }

            _logger.LogTrace($"Client {id} disconnected");

            lock (_lock) _listClients.Remove(id);
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        private void Broadcast(string data)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(data + Environment.NewLine);

            lock (_lock)
            {
                foreach (TcpClient c in _listClients.Values)
                {
                    NetworkStream stream = c.GetStream();

                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
