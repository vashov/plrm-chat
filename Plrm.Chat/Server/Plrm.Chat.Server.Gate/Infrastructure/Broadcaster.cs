using Plrm.Chat.Shared.Models;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Plrm.Chat.Server.Gate.Infrastructure
{
    internal static class Broadcaster
    {
        public static void SendToAuthorizedUsers(ChatMessage message, IEnumerable<ChatClient> clients)
        {
            foreach (ChatClient chatClient in clients)
            {
                if (chatClient.User == null)
                    continue;

                NetworkStream stream = chatClient.Client.GetStream();
                stream.Write(message.Content);
            }
        }
    }
}
