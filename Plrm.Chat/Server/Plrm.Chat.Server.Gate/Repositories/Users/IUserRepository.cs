using Plrm.Chat.Server.Gate.Infrastructure;
using Plrm.Chat.Server.Gate.Repositories.Users.Models;

namespace Plrm.Chat.Server.Gate.Repositories.Users
{
    public interface IUserRepository
    {
        public User FindByLogin(string login);
        public OperationResult<User> Create(string login, string password);
    }
}
