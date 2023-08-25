using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace PROJECT_g0la
{
    internal class Program
    {
        public static DiscordSocketClient _client;
        public static Config _config;
        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            await InitializeProgram();

            await Log(new LogMessage(LogSeverity.Info, "Client", $"Starting"));
            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };
            _client = new DiscordSocketClient(config);

            _client.Ready += Client_Ready;
            _client.MessageReceived += Client_MessageReceived;

            var token = _config.BOT_KEY;


            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async Task InitializeProgram()
        {
            Console.WriteLine(new LogMessage(LogSeverity.Info, "Config", $"Starting config load"));
            Config config = await Services.GetConfig();
            if (config is null)
            {
                await InitalizationError($"ERROR Loading {Path.Combine(Directory.GetCurrentDirectory(), "config.json")} | (FILE DOES NOT EXIST)");
            }

            Console.WriteLine(new LogMessage(LogSeverity.Info, "Config", $"Loading BOT_KEY"));
            if (config.BOT_KEY == "" || config.BOT_KEY == "REPLACE_WITH_YOUR_KEY" || config.BOT_KEY is null)
            {
                await InitalizationError($"ERROR Loading valid value \"BOT_KEY\" | VALUE: {config.BOT_KEY}");
            }

            Console.WriteLine(new LogMessage(LogSeverity.Info, "Config", $"Loading CHANNEL_QUEUE"));
            if (config.CHANNEL_QUEUE == 0)
            {
                await InitalizationError($"ERROR Loading valid value \"CHANNEL_QUEUE\" | VALUE: {config.CHANNEL_QUEUE}");
            }

            Console.WriteLine(new LogMessage(LogSeverity.Info, "Config", $"Loading CATEGORY_GAMES"));
            if (config.CATEGORY_GAMES == 0)
            {
                await InitalizationError($"ERROR Loading valid value \"CATEGORY_GAMES\" | VALUE: {config.CATEGORY_GAMES}");
            }

            _config = config;
            Console.WriteLine(new LogMessage(LogSeverity.Info, "Config", $"Config load success"));
        }

        private async Task InitalizationError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(new LogMessage(LogSeverity.Info, "Config", $"{message}"));
            Console.ResetColor();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            Environment.Exit(0);
            return;
        }

        private async Task Client_MessageReceived(SocketMessage message)
        {
            if (message.Channel.Id == _config.CHANNEL_QUEUE && message.Author.Id != _client.CurrentUser.Id)
            {
                await message.DeleteAsync();
            }
        }

        public async Task Client_Ready()
        {
            await Log(new LogMessage(LogSeverity.Info, "Client", $"Ready"));
            await Start_Bot();
        }

        public async Task Start_Bot()
        {
            QueueHandler queue = new();
            await queue.StartQueue();
        }


        public static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}