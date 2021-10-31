using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plrm.Chat.Shared.Validators;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Plrm.Chat.Client
{
    class Program
    {
        static string _errorMsg = null;

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole())
                .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Trace);

            services.AddSingleton<AuthManager>();
        }

        static async Task Main(string[] args)
        {
            IPAddress serverAddress = IPAddress.Parse("127.0.0.1");
            int serverPort = 5000;

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Chat>>();
            var authManager = serviceProvider.GetRequiredService<AuthManager>();

            var chat = new Chat(logger, authManager, serverAddress, serverPort);

            try
            {
                while (authManager.IsLoggedIn != true)
                {
                    InputCredentials(authManager);

                    if (!chat.Connect())
                    {
                        return;
                    }

                    var successLogin = await chat.LogInToChat();
                    if (!successLogin.Value)
                    {
                        UIOutput.WriteLineError($"Authorization error: {_errorMsg}");
                        chat.Disconnect();
                    }
                }

                string s;
                while (!string.IsNullOrEmpty((s = Console.ReadLine())))
                {
                    chat.SendMessage(s);
                }

            }
            finally
            {
                chat.Disconnect();
            }

            UIOutput.WriteLineSystem("Enter any KEY to exit...");
            Console.ReadKey();
        }

        static void InputCredentials(AuthManager authManager)
        {
            authManager.ResetCredentials();

            while (!UserCredentialsValidator.IsLoginValid(authManager.Login))
            {
                UIOutput.WriteUserInteraction("Enter Login: ");
                authManager.Login = Console.ReadLine();
            }

            while (!UserCredentialsValidator.IsPasswordValid(authManager.Password))
            {
                UIOutput.WriteUserInteraction("Enter password: ");
                authManager.Password = Console.ReadLine();
            }
        }
    }
}
