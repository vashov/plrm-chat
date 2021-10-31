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
using System.Threading.Tasks;

namespace Plrm.Chat.Server.Gate
{
    class Chat
    {

        private readonly ILogger<Chat> _logger;
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPAddress _address;
        private readonly int _port;
        private readonly int _countOfLastMesssagesToConnectedUser;
        private TcpListener _listener;

        private readonly object _lock = new object();
        private readonly Dictionary<int, ChatClient> _listClients = new Dictionary<int, ChatClient>();

        public Chat(
            ILogger<Chat> logger,
            IMessageRepository messageRepository,
            IUserRepository userRepository,
            IPAddress address,
            int port,
            int countOfLastMesssagesToConnectedUser)
        {
            _logger = logger;
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _address = address;
            _port = port;
            _countOfLastMesssagesToConnectedUser = countOfLastMesssagesToConnectedUser;
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

                int clientId = count;
                Task.Run(() => HandleClient(clientId));
                count++;
            }
        }

        private async void HandleClient(int id)
        {
            ChatClient chatClient;

            lock (_lock)
            {
                chatClient = _listClients[id];
            }

            OperationResult<(User User, bool IsNewUser)> result = await AuthenticateClient(chatClient);
            if (!result.IsOk)
            {
                await chatClient.SendAuthorizeResponseError(result.ErrorMessage);

                lock (_lock) _listClients.Remove(id);
                chatClient.Disconnect();
                return;
            }

            chatClient.Authorize(result.Result.User);
            await chatClient.SendAuthorizeResponseSuccess();

            if (_countOfLastMesssagesToConnectedUser > 0)
            {
                List<ChatMessage> messages = _messageRepository.List(lastCount: _countOfLastMesssagesToConnectedUser);

                foreach (ChatMessage m in messages)
                {
                    await Task.Delay(10);
                    await chatClient .SendMessage(m);
                }
            }

            string userConnectedMessage = result.Result.IsNewUser ? $"{chatClient.User.Login} registered and connected"
                : $"{chatClient.User.Login} connected";

            byte[] messageContent = Encoding.UTF8.GetBytes(userConnectedMessage);
            ChatMessage message = _messageRepository.Create(chatClient.User.Id, messageContent);

            // System message about new user connected.
            message.UserLogin = "system";
            await Broadcast(message);

            while (true)
            {
                OperationResult<byte[]> readResult = await chatClient.ReadMessage();

                if (!readResult.IsOk)
                {
                    _logger.LogWarning(readResult.ErrorMessage);
                    break;
                }

                message = _messageRepository.Create(chatClient.User.Id, message: readResult.Result);

                // We may create ChatMessageDto and map data from DB layer to Api (via AutoMapper)
                message.UserLogin = chatClient.User.Login;
                await Broadcast(message);

                string data = Encoding.UTF8.GetString(readResult.Result);
                _logger.LogTrace(data);
            }

            lock (_lock) _listClients.Remove(id);
            chatClient.Disconnect();

            _logger.LogTrace($"Client {id} disconnected");
        }

        private async Task<OperationResult<(User User, bool IsNewUser)>> AuthenticateClient(ChatClient chatClient)
        {
            OperationResult<UserCredentials> readResult = await chatClient.ReadAuthenticationMessage();

            if (!readResult.IsOk)
            {
                _logger.LogWarning(readResult.ErrorMessage);
                return OperationResult<(User, bool)>.Error("Invalid data.");
            }

            UserCredentials userCredentials = readResult.Result;

            if (!UserCredentialsValidator.IsValid(userCredentials.Login, userCredentials.Password))
            {
                _logger.LogTrace($"Client {chatClient.Id} UserCredentials Invalid");
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

        private async Task Broadcast(ChatMessage message)
        {
            //lock (_lock)
            //{
            //    Broadcaster.SendToAuthorizedUsers(message, _listClients.Values);
            //}
            await Broadcaster.SendToAuthorizedUsers(message, _listClients.Values);
        }
    }
}
