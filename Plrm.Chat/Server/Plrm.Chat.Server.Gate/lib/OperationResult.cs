namespace Plrm.Chat.Server.Gate.lib
{
    public class OperationResult<T>
    {
        public T Result { get; private set; }
        public bool IsOk => string.IsNullOrEmpty(ErrorMessage);
        public string ErrorMessage { get; private set; }

        private OperationResult()
        {
        }

        public static OperationResult<T> Ok(T data)
        {
            return new OperationResult<T>
            {
                Result = data,
                ErrorMessage = string.Empty
            };
        }

        public static OperationResult<T> Error(string message)
        {
            return new OperationResult<T>
            {
                Result = default,
                ErrorMessage = message
            };
        }
    }
}
