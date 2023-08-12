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
        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };
            _client = new DiscordSocketClient(config);

            _client.Log += Log;

            _client.Ready += Client_Ready;
            _client.MessageReceived += Client_MessageReceived;

            var token = "";


            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async Task Client_MessageReceived(SocketMessage message)
        {
            if (message.Channel.Id == 1139615881194897468 && message.Author.Id != _client.CurrentUser.Id)
            {
                await message.DeleteAsync();
            }
        }

        public async Task Client_Ready()
        {
            await Start_Bot();
        }

        public async Task Start_Bot()
        {
            QueueHandler queue = new();
            await queue.StartQueue();
        }


        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}