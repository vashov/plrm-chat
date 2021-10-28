using Plrm.Chat.Shared.Models;
using System.Collections.Generic;

namespace Plrm.Chat.Server.Gate.Repositories.Messages
{
    public interface IMessageRepository
    {
        public ChatMessage Create(long userId, byte[] message);
        public List<ChatMessage> List(int? lastCount = 0);
    }
}
