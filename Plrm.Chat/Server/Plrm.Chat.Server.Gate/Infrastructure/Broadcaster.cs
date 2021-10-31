using Plrm.Chat.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plrm.Chat.Server.Gate.Infrastructure
{
    internal static class Broadcaster
    {
        public static async Task SendToAuthorizedUsers(ChatMessage message, IEnumerable<ChatClient> clients)
        {
            foreach (ChatClient chatClient in clients)
            {
                if (!chatClient.IsAuthorized)
                    continue;

                await chatClient.SendMessage(message);
            }
        }
    }
}
