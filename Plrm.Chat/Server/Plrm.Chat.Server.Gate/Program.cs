using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plrm.Chat.Server.Gate.Repositories.Messages;
using System.Net;

namespace Plrm.Chat.Server.Gate
{
    class Program
    {
        

        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Chat>>();
            var messageRepository = serviceProvider.GetRequiredService<IMessageRepository>();

            var chat = new Chat(logger, messageRepository, IPAddress.Any, 5000);
            chat.Start();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole())
                .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Trace);

            services.AddSingleton<IMessageRepository, MessageRepository>();
        }
    }
}
