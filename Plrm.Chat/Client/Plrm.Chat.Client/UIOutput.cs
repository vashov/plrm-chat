using System;

namespace Plrm.Chat.Client
{
    static class UIOutput
    {
        private const ConsoleColor COLOR_OF_CHAT_MESSAGE_FROM_USER = ConsoleColor.Blue;
        private const ConsoleColor COLOR_OF_SYSTEM_INFO_MESSAGE = ConsoleColor.Green;
        private const ConsoleColor COLOR_OF_ERROR_MESSAGE = ConsoleColor.Red;
        private const ConsoleColor COLOR_OF_USER_INTERACTION = ConsoleColor.White;

        public static void WriteLineChatMessage(string username, string message)
        {
            var color = Console.ForegroundColor;

            Console.ForegroundColor = COLOR_OF_CHAT_MESSAGE_FROM_USER;
            Console.WriteLine($"{username}: {message}");

            Console.ForegroundColor = color;
        }

        public static void WriteLineSystem(string message)
        {
            var color = Console.ForegroundColor;

            Console.ForegroundColor = COLOR_OF_SYSTEM_INFO_MESSAGE;
            Console.WriteLine(message);

            Console.ForegroundColor = color;
        }

        public static void WriteLineError(string message)
        {
            var color = Console.ForegroundColor;

            Console.ForegroundColor = COLOR_OF_ERROR_MESSAGE;
            Console.WriteLine(message);

            Console.ForegroundColor = color;
        }

        public static void WriteUserInteraction(string message)
        {
            var color = Console.ForegroundColor;

            Console.ForegroundColor = COLOR_OF_USER_INTERACTION;
            Console.Write(message);

            Console.ForegroundColor = color;
        }
    }
}
