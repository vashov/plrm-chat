using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plrm.Chat.Client
{
    class AuthManager
    {
        public string Login { get; set; }
        public string Password { get; set; }

        public bool? IsLoggedIn { get; private set; }
        public string LoginError { get; private set; }

        public void SetStateLoginFailed(string error)
        {
            IsLoggedIn = false;
            LoginError = error;
        }

        public void SetStateLoginSuccess()
        {
            LoginError = null;
            IsLoggedIn = true;
        }

        public void ResetStateLogin()
        {
            LoginError = null;
            IsLoggedIn = null;
        }

        public void ResetCredentials()
        {
            Login = null;
            Password = null;
        }

    }
}
