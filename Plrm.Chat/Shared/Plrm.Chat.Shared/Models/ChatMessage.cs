using System;

namespace Plrm.Chat.Shared.Models
{
    public class ChatMessage
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserLogin { get; set; }
        public byte[] Content { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
