namespace Plrm.Chat.Shared.Validators
{
    public static class UserCredentialsValidator
    {
        public static bool IsValid(string login, string password)
        {
            return !string.IsNullOrWhiteSpace(login)
                && !string.IsNullOrWhiteSpace(password);
        }
    }
}
