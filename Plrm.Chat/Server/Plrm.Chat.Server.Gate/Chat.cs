using Microsoft.Extensions.Logging;
using Plrm.Chat.Server.Gate.Infrastructure;
using Plrm.Chat.Server.Gate.Repositories.Messages;
using Plrm.Chat.Server.Gate.Repositories.Users;
using Plrm.Chat.Server.Gate.Repositories.Users.Models;
using Plrm.Chat.Shared.Models;
using Plrm.Chat.Shared.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Plrm.Chat.Server.Gate
{
    class Chat
    {

        private readonly ILogger<Chat> _logger;
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPAddress _address;
        private readonly int _port;

        private TcpListener _listener;

        private readonly object _lock = new object();
        private readonly Dictionary<int, ChatClient> _listClients = new Dictionary<int, ChatClient>();

        public Chat(
            ILogger<Chat> logger,
            IMessageRepository messageRepository,
            IUserRepository userRepository,
            IPAddress address,
            int port)
        {
            _logger = logger;
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _address = address;
            _port = port;
        }

        public void Start()
        {
            _listener = new TcpListener(_address, _port);
            _listener.Start();

            int count = 1;

            while (true)
            {
                TcpClient client = _listener.AcceptTcpClient();

                lock (_lock)
                {
                    var chatClient = new ChatClient(count, client);
                    _listClients.Add(count, chatClient);
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
            ChatClient chatClient;

            lock (_lock)
            {
                chatClient = _listClients[id];
            }

            TcpClient client = chatClient.Client;

            OperationResult<(User User, bool IsNewUser)> result = AuthenticateClient(id, client);
            if (!result.IsOk)
            {
                chatClient.SendAuthorizeResponseError(result.ErrorMessage);

                lock (_lock) _listClients.Remove(id);
                client.Client.Shutdown(SocketShutdown.Both);
                client.Close();
                return;
            }

            chatClient.Authorize(result.Result.User);
            chatClient.SendAuthorizeResponseSuccess();

            string userConnectedMessage = result.Result.IsNewUser ? $"{chatClient.User.Login} registered and connected"
                : $"{chatClient.User.Login} connected";

            byte[] messageContent = Encoding.UTF8.GetBytes(userConnectedMessage);
            ChatMessage message = _messageRepository.Create(chatClient.User.Id, messageContent);
            Broadcast(message);

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

                message = _messageRepository.Create(chatClient.User.Id, content);
                Broadcast(message);

                _logger.LogTrace(data);
            }

            _logger.LogTrace($"Client {id} disconnected");

            lock (_lock) _listClients.Remove(id);
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        private OperationResult<(User User, bool IsNewUser)> AuthenticateClient(long id, TcpClient client)
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
                return OperationResult<(User, bool)>.Error("User credentials data is empty.");
            }

            var content = buffer.Take(byte_count).ToArray();
            string data = Encoding.UTF8.GetString(content);

            UserCredentials userCredentials;
            try
            {
                userCredentials = JsonSerializer.Deserialize<UserCredentials>(data);
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Client {id} UserCredentials Deserialize: {e}");
                return OperationResult<(User, bool)>.Error("Invalid user credentials data.");
            }

            if (!UserCredentialsValidator.IsValid(userCredentials.Login, userCredentials.Password))
            {
                _logger.LogTrace($"Client {id} UserCredentials Invalid");
                return OperationResult<(User, bool)>.Error("Login or password has invalid format.");
            }

            var user = _userRepository.FindByLogin(userCredentials.Login);
            if (user != null)
            {
                if (user.Password != userCredentials.Password)
                {
                    return OperationResult<(User, bool)>.Error("Invalid login or password.");
                }

                return OperationResult<(User, bool)>.Ok((user, false));
            }

            OperationResult<User> createResult = _userRepository
                .Create(userCredentials.Login, userCredentials.Password);

            return OperationResult<(User, bool)>.Ok((createResult.Result, true));
        }

        private void Broadcast(ChatMessage message)
        {
            lock (_lock)
            {
                Broadcaster.SendToAuthorizedUsers(message, _listClients.Values);
            }
        }
    }
}
