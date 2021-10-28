using System;

namespace Plrm.Chat.Server.Gate.Repositories.Users.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
