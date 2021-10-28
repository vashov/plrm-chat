using Microsoft.Extensions.Logging;
using Plrm.Chat.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plrm.Chat.Server.Gate.Repositories.Messages
{
    class MessageRepository : IMessageRepository
    {
        private readonly ILogger<MessageRepository> _logger;

        private object _lock = new object();

        private LinkedList<ChatMessage> _messages = new LinkedList<ChatMessage>();

        public MessageRepository(ILogger<MessageRepository> logger)
        {
            _logger = logger;
        }

        public ChatMessage Create(long userId, byte[] content)
        {
            var newMessage = new ChatMessage
            {
                UserId = userId,
                Content = content
            };

            lock(_lock)
            {
                ChatMessage lastMessage = _messages.LastOrDefault();
                newMessage.Id = lastMessage == null ? 1 : lastMessage.Id + 1;
                newMessage.CreatedAt = DateTimeOffset.UtcNow;

                _messages.AddLast(newMessage);
            }

            return newMessage;
        }

        public List<ChatMessage> List(int? lastCount = null)
        {
            if (lastCount.HasValue && _messages.Count > lastCount.Value)
                return _messages.TakeLast(lastCount.Value).ToList();

            return _messages.ToList();
        }
    }
}
