using Microsoft.Extensions.Logging;
using Plrm.Chat.Server.Gate.Repositories.Messages;
using Plrm.Chat.Shared.Models;
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
        private readonly IMessageRepository _messageRepository;
        private readonly IPAddress _address;
        private readonly int _port;

        private TcpListener _listener;

        private readonly object _lock = new object();
        private readonly Dictionary<int, TcpClient> _listClients = new Dictionary<int, TcpClient>();

        public Chat(
            ILogger<Chat> logger,
            IMessageRepository messageRepository,
            IPAddress address,
            int port)
        {
            _logger = logger;
            _messageRepository = messageRepository;
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

                var content = buffer.Take(byte_count).ToArray();
                string data = Encoding.UTF8.GetString(content);

                // TODO userId
                var message = _messageRepository.Create(userId: 0, content);
                Broadcast(message);

                _logger.LogTrace(data);
            }

            _logger.LogTrace($"Client {id} disconnected");

            lock (_lock) _listClients.Remove(id);
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        private void Broadcast(ChatMessage message)
        {
            lock (_lock)
            {
                foreach (TcpClient c in _listClients.Values)
                {
                    NetworkStream stream = c.GetStream();

                    stream.Write(message.Content);
                }
            }
        }
    }
}
