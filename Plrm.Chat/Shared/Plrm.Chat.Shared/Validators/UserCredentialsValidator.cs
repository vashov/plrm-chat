namespace Plrm.Chat.Shared.Validators
{
    public static class UserCredentialsValidator
    {
        public static bool IsValid(string login, string password)
        {
            return IsLoginValid(login)
                && IsPasswordValid(password);
        }

        public static bool IsLoginValid(string login)
        {
            return !string.IsNullOrWhiteSpace(login);
        }

        public static bool IsPasswordValid(string password)
        {
            return !string.IsNullOrWhiteSpace(password);
        }
    }
}
