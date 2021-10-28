using Microsoft.Extensions.Logging;
using Plrm.Chat.Server.Gate.lib;
using Plrm.Chat.Server.Gate.Repositories.Users.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plrm.Chat.Server.Gate.Repositories.Users
{
    class UserRepository : IUserRepository
    {
        private readonly ILogger<UserRepository> _logger;

        private object _lock = new object();

        private LinkedList<User> _users = new LinkedList<User>();

        public UserRepository(ILogger<UserRepository> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Return null if user already exists
        /// </summary>
        public OperationResult<User> Create(string login, string password)
        {
            if (!IsValid(login, password))
            {
                return OperationResult<User>.Error("Invalid login or password format");
            }

            // TODO: password hash and salt
            var newUser = new User
            {
                Login = login,
                Password = password,
            };

            lock (_lock)
            {
                var user = _users.FirstOrDefault(u => u.Login == login);
                if (user != null)
                {
                    return OperationResult<User>.Error("User already exists");
                }

                var lastUser = _users.LastOrDefault();
                newUser.Id = lastUser == null ? 1 : lastUser.Id + 1;
                newUser.CreatedAt = DateTimeOffset.UtcNow;
                _users.AddLast(newUser);
            }

            return OperationResult<User>.Ok(newUser);
        }

        public User FindByLogin(string login)
        {
            _users.FirstOrDefault(u => u.Login == login);
            throw new NotImplementedException();
        }

        private bool IsValid(string login, string password)
        {
            return !string.IsNullOrWhiteSpace(login)
                && !string.IsNullOrWhiteSpace(password);
        }
    }
}
