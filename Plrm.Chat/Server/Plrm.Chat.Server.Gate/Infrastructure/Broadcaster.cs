using Plrm.Chat.Shared.Models;
using System.Collections.Generic;

namespace Plrm.Chat.Server.Gate.Infrastructure
{
    internal static class Broadcaster
    {
        public static void SendToAuthorizedUsers(ChatMessage message, IEnumerable<ChatClient> clients)
        {
            foreach (ChatClient chatClient in clients)
            {
                if (!chatClient.IsAuthorized)
                    continue;

                chatClient.SendMessage(message);
            }
        }
    }
}
