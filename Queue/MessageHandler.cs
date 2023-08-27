using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using PROJECT_g0la.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static PROJECT_g0la.QueueHandler;

namespace PROJECT_g0la
{
    internal class MessageHandler
    {
        public static RestUserMessage? _queueMessage;
        public static RestUserMessage? _queuePopMessage;
        private static readonly DiscordSocketClient _client = Program._client;
        public static QueueHandler? _handler;
        private static Dictionary<ulong, bool> _duoChecks = new();
        private static ISocketMessageChannel? _channel;
        public static Dictionary<ulong, QueuePopSuccessObject> _activeGames = new();

        public static async Task StartQueue()
        {
            _client.ButtonExecuted += ButtonHandler;

            _client.SlashCommandExecuted += SlashCommandHandler;

            _client.ReactionAdded += ReactionAddedHandler;

            _client.MessageReceived += MessageReceivedHandler;

            await CreateCommands();


            _channel = _client.GetChannel(Program._config.CHANNEL_QUEUE) as ISocketMessageChannel;

            await PurgeChannel();

            await SendQueueMessage();
        }

        private static async Task MessageReceivedHandler(SocketMessage arg)
        {
            if (!_activeGames.ContainsKey(arg.Channel.Id) || arg.Attachments.Count <= 0) { return; }
            foreach (var attachment in arg.Attachments)
            {
                Console.WriteLine(attachment.Filename);
                if (attachment.Filename.Contains("rofl"))
                {
                    using (var client = new WebClient())
                    {
                        client.DownloadFileAsync(new Uri(attachment.Url), "tempgame.rofl");
                        client.DownloadFileCompleted += (x, y) => RoflDownload_Finished(x, y, arg.Channel.Id);
                    }
                }
            }
        }

        private static async void RoflDownload_Finished(object? sender, System.ComponentModel.AsyncCompletedEventArgs e, ulong channelID)
        {
            ReplayObject replay = Services.ROFLHandler.ParseROFL("tempgame.rofl");

            if (!CheckRofl(replay, _activeGames[channelID])) { return; }

            ISocketMessageChannel channel = (ISocketMessageChannel)_client.GetChannel(channelID);

            if ((replay.statsJson[0].TEAM == "100" && replay.statsJson[0].WIN != "Fail") || (replay.statsJson[0].TEAM == "200" && replay.statsJson[0].WIN == "Fail"))
            {
                // BLUE WIN
                await channel.SendMessageAsync("GAME MARKED AS WON ON BLUESIDE");
            }
            if ((replay.statsJson[0].TEAM == "200" && replay.statsJson[0].WIN != "Fail") || (replay.statsJson[0].TEAM == "100" && replay.statsJson[0].WIN == "Fail"))
            {
                // RED WIN
                await channel.SendMessageAsync("GAME MARKED AS WON ON REDSIDE");
            }
        }

        private static bool CheckRofl(ReplayObject replay, QueuePopSuccessObject activeGame)
        {
            List<string> summonerNames = new();
            foreach(Summoner summoner in replay.statsJson)
            {
                if (summoner.TEAM == "100")
                {
                    summonerNames.Add(summoner.NAME);
                }
            }

            bool contains = activeGame.Teams[Side.Blue].AllPlayers.All(player => summonerNames.Contains(player.Summoner));
            return contains;
        }

        private static async Task ReactionAddedHandler(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
        {
            if (arg3.UserId == _client.CurrentUser.Id) { return; }
            if (arg3.Emote.Name == "✅") { await _handler.PlayerAccept(arg3.UserId); }
            if (arg3.Emote.Name == "❌") { await _handler.PlayerDecline(arg3.UserId); }
            
        }

        private static async Task PurgeChannel()
        {
            if (_channel != null)
            {
                // Get all messages in the channel
                var messages = await _channel.GetMessagesAsync().FlattenAsync();

                foreach (var message in messages)
                {
                    // Delete each message
                    await message.DeleteAsync();
                }
            }
        }

        public static async Task SendQueuePopMessage(QueuePopObject queuePopObject)
        {
            await PurgeChannel();

            string mentions = string.Join("", queuePopObject.AllRoles.SelectMany(list => list).Select(player => $"<@{player.DiscordID}>"));

            string popMessage = $"||{mentions}||\n```ini\n" +
                 $"[MATCH FOUND]" +
                 $"\n{string.Join("\n", queuePopObject.AllRoles.SelectMany(list => list).Select(player => $"{_client.GetUser(player.DiscordID) + (player.QueueAccepted ? "✅" : "")}"))}```";


            _queuePopMessage = await _channel.SendMessageAsync(popMessage);
            var emoji = new Discord.Emoji("✅");
            var emoji2 = new Discord.Emoji("❌");
            await _queuePopMessage.AddReactionAsync(emoji);
            await _queuePopMessage.AddReactionAsync(emoji2);

            await UpdateQueuePopMessage(queuePopObject);
            
        }

        public static async Task UpdateQueuePopMessage(QueuePopObject queuePopObject)
        {
            string mentions = string.Join("", queuePopObject.AllRoles.SelectMany(list => list).Select(player => $"<@{player.DiscordID}>"));

            string popMessage = $"||{mentions}||\n```ini\n" +
                $"[MATCH FOUND]" +
                $"\n{string.Join("\n", queuePopObject.AllRoles.SelectMany(list => list).Select(player => $"{_client.GetUser(player.DiscordID) + (player.QueueAccepted ? "✅" : "")}"))}```";

            await _queuePopMessage.ModifyAsync(msg => msg.Content = popMessage);
        }

        public static async Task CancelQueuePop(SocketUser user)
        {
            await _channel.SendMessageAsync($"<@{user.Id}> declined. Cancelling match..");
            await TaskDelay(5);
            await PurgeChannel();
            await SendQueueMessage();

        }

        public static async Task HandleQueuePopAccept(QueuePopSuccessObject popObject)
        {
            var guild = _client.GetGuild(Program._config.GUILD_ID);

            SocketCategoryChannel category = guild.GetCategoryChannel(Program._config.CATEGORY_GAMES);

            RestTextChannel newChannel = await guild.CreateTextChannelAsync($"game", tcp => tcp.CategoryId = category.Id);
            RestVoiceChannel vc1 = await guild.CreateVoiceChannelAsync("BLUE", tcp => tcp.CategoryId = category.Id);
            RestVoiceChannel vc2 = await guild.CreateVoiceChannelAsync("RED", tcp => tcp.CategoryId = category.Id);

            var allowViewPermission = new OverwritePermissions(viewChannel: PermValue.Allow);
            var dontAllowViewPermission = new OverwritePermissions(viewChannel: PermValue.Deny);

            foreach (Team team in popObject.Teams.Values)
            {
                //foreach (Player player in team.AllPlayers)
                //{
                //    await newChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, dontAllowViewPermission);
                //    await newChannel.AddPermissionOverwriteAsync(_client.GetUser(player.DiscordID), allowViewPermission);
                //    if (player.Side == Side.Blue)
                //    {
                //        await vc1.AddPermissionOverwriteAsync(guild.EveryoneRole, dontAllowViewPermission);
                //        await vc1.AddPermissionOverwriteAsync(_client.GetUser(player.DiscordID), allowViewPermission);
                //    }
                //    else
                //    {
                //        await vc2.AddPermissionOverwriteAsync(guild.EveryoneRole, dontAllowViewPermission);
                //        await vc2.AddPermissionOverwriteAsync(_client.GetUser(player.DiscordID), allowViewPermission);
                //    }
                //}
            }

            //string message = "```ini\n[MATCH]\n\n" +
            //    "[BLUE]\n" +
            //    $"Top:     {_client.GetUser(popObject.Teams[Side.Blue].Top.DiscordID)}\n" +
            //    $"Jungle:  {_client.GetUser(popObject.Teams[Side.Blue].Jungle.DiscordID)}\n" +
            //    $"Mid:     {_client.GetUser(popObject.Teams[Side.Blue].Mid.DiscordID)}\n" +
            //    $"Bottom:  {_client.GetUser(popObject.Teams[Side.Blue].Bottom.DiscordID)}\n" +
            //    $"Support: {_client.GetUser(popObject.Teams[Side.Blue].Support.DiscordID)}\n" +
            //    $"\n[RED]\n" +
            //    $"Top:     {_client.GetUser(popObject.Teams[Side.Red].Top.DiscordID)}\n" +
            //    $"Jungle:  {_client.GetUser(popObject.Teams[Side.Red].Jungle.DiscordID)}\n" +
            //    $"Mid:     {_client.GetUser(popObject.Teams[Side.Red].Mid.DiscordID)}\n" +
            //    $"Bottom:  {_client.GetUser(popObject.Teams[Side.Red].Bottom.DiscordID)}\n" +
            //    $"Support: {_client.GetUser(popObject.Teams[Side.Red].Support.DiscordID)}\n" +
            //    $"```" +
            //    $"\n[Blue OPGG](https://www.op.gg/multisearch/euw?summoners={string.Join(",", popObject.Teams[Side.Blue].AllPlayers.Select(player => player.Summoner.Replace(" ", "")))})" +
            //    $"\n[Red OPGG](https://www.op.gg/multisearch/euw?summoners={string.Join(",", popObject.Teams[Side.Red].AllPlayers.Select(player => player.Summoner.Replace(" ", "")))})" +
            //    $"";

            string message = "```ini\n[MATCH]\n\n" +
                "[BLUE]\n" +
                $"Top:     {popObject.Teams[Side.Blue].Top.Summoner}\n" +
                $"Jungle:  {popObject.Teams[Side.Blue].Jungle.Summoner}\n" +
                $"Mid:     {popObject.Teams[Side.Blue].Mid.Summoner}\n" +
                $"Bottom:  {popObject.Teams[Side.Blue].Bottom.Summoner}\n" +
                $"Support: {popObject.Teams[Side.Blue].Support.Summoner}\n" +
                $"\n[RED]\n" +
                $"Top:     {popObject.Teams[Side.Red].Top.Summoner}\n" +
                $"Jungle:  {popObject.Teams[Side.Red].Jungle.Summoner}\n" +
                $"Mid:     {popObject.Teams[Side.Red].Mid.Summoner}\n" +
                $"Bottom:  {popObject.Teams[Side.Red].Bottom.Summoner}\n" +
                $"Support: {popObject.Teams[Side.Red].Support.Summoner}\n" +
                $"```" +
                $"\n[Blue OPGG](https://www.op.gg/multisearch/euw?summoners={string.Join(",", popObject.Teams[Side.Blue].AllPlayers.Select(player => player.Summoner.Replace(" ", "")))})" +
                $"\n[Red OPGG](https://www.op.gg/multisearch/euw?summoners={string.Join(",", popObject.Teams[Side.Red].AllPlayers.Select(player => player.Summoner.Replace(" ", "")))})" +
                $"";
            await newChannel.SendMessageAsync(message);

            await PurgeChannel();
            await SendQueueMessage();

            _activeGames.Add(newChannel.Id, popObject);
        }

        public static async Task SendQueueMessage()
        {
            var builder = new ComponentBuilder()
                .WithButton("Top", "queue-top")
                .WithButton("Jungle", "queue-jungle")
                .WithButton("Mid", "queue-mid")
                .WithButton("Bottom", "queue-bottom")
                .WithButton("Support", "queue-support")
                .WithButton("X", "queue-leave");

            _queueMessage = await _channel.SendMessageAsync("", components: builder.Build());
            await HandleQueueMessage();
        }

        private async static Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "duo":
                    await HandleDuoCommand(command);
                    break;
                case "remove-player":
                    await HandleRemovePlayerCommand(command);
                    break;
                case "clear-queue":
                    await HandleClearQueueCommand(command);
                    break;
            }
        }

        private static async Task HandleRemovePlayerCommand(SocketSlashCommand command)
        {
            ulong parsedUserID = 0;

            foreach (var option in command.Data.Options)
            {
                string userId = option.Value.ToString();
                if (userId != "" && userId.Contains("<@")) { parsedUserID = (ulong)Int64.Parse(userId.Replace("<@", "").Replace(">", "")); }
            }

            if (!_handler._players.ContainsKey(parsedUserID.ToString())) { await command.RespondAsync("User currently not in Queue.", ephemeral:true); return; }
            await _handler.LeaveQueue(_client.GetUser(parsedUserID));
            await command.RespondAsync($"Successfully removed user <@{parsedUserID}>", ephemeral:true);
        }

        private static async Task HandleClearQueueCommand(SocketSlashCommand command)
        {
            await _handler.queue.ClearQueue();

            await command.RespondAsync("Successfully cleared Queue.", ephemeral: true);
        }

        private static async Task HandleDuoCommand(SocketSlashCommand command)
        {
            Role[] roleArray = (Role[])Enum.GetValues(typeof(Role));
            Role myRole = new();
            string myDuoId = "";
            ulong parsedDuoId = 0;
            Role myDuosRole = new();

            foreach (var option in command.Data.Options)
            {
                switch (option.Name)
                {
                    case "your-role":
                        myRole = roleArray[(Int64)option.Value];
                        break;
                    case "your-duo":
                        myDuoId = option.Value.ToString();
                        break;
                    case "your-duos-role":
                        myDuosRole = roleArray[(Int64)option.Value];
                        break;
                }
            }
            if (myDuoId != "" && myDuoId.Contains("<@")) { parsedDuoId = (ulong)Int64.Parse(myDuoId.Replace("<@", "").Replace(">", "")); }
            SocketUser duoUser = _client.GetUser(parsedDuoId);
            if (duoUser is null) { await command.RespondAsync("Duo user not found.", ephemeral: true); return; }
            if (myRole == myDuosRole) { await command.RespondAsync("You cannot duo the same 2 roles.", ephemeral: true); return; }
            if (parsedDuoId == command.User.Id) { await command.RespondAsync("You cannot duo yourself.", ephemeral: true); return; }


            var builder = new ComponentBuilder()
                .WithButton("Accept", "duo-accept", ButtonStyle.Success)
                .WithButton("Decline", "duo-decline", ButtonStyle.Danger);

            await command.RespondAsync($"<@{command.User.Id}> wants to duo with {myDuoId}. They will need to accept", components: builder.Build());

            var handleAcceptingTask = Task.Run(async () =>
            {
                _duoChecks.Add(parsedDuoId, false);
                await TaskDelay(20, parsedDuoId);

                if (_duoChecks[parsedDuoId])
                {
                    Tuple<Tuple<SocketUser, Role>, Tuple<SocketUser, Role>> duo = new
                    (
                        new Tuple<SocketUser, Role>(command.User, myRole),
                        new Tuple<SocketUser, Role>(duoUser, myDuosRole)
                    );
                    await _handler.QueueDuo(duo);
                    RestInteractionMessage responseMessage = await command.GetOriginalResponseAsync();
                    await responseMessage.DeleteAsync();
                } else
                {
                    RestInteractionMessage responseMessage = await command.GetOriginalResponseAsync();
                    IUserMessage newResponse = await responseMessage.ReplyAsync("Duo request not accepted, deleting..");
                    await Task.Delay(3000);
                    await responseMessage.DeleteAsync();
                    await newResponse.DeleteAsync();
                }
            });

            
            
        }

        public static async Task TaskDelay(int time, ulong duoId = 0)
        {
            for (int i = 0; i < time; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                if (duoId != 0 && _duoChecks[duoId]) { break; }
            }
        }

        public static async Task HandleDuoAccept(SocketMessageComponent component)
        {
            if (!_duoChecks.ContainsKey(component.User.Id)) { await component.DeferAsync(); return; }
            _duoChecks[component.User.Id] = true;
        }

        public static async Task CreateCommands()
        {
            var duoCommand = new SlashCommandBuilder()
                .WithName("duo")
                .WithDescription("Duoqueue with someone")
                    .AddOption(new SlashCommandOptionBuilder().WithName("your-role")
                    .WithDescription("What role you will be Queueing as")
                    .WithRequired(true)
                    .AddChoice("Top", 0)
                    .AddChoice("Jungle", 1)
                    .AddChoice("Mid", 2)
                    .AddChoice("Bottom", 3)
                    .AddChoice("Support", 4)
                    .WithType(ApplicationCommandOptionType.Integer))
                    .AddOption(new SlashCommandOptionBuilder().WithName("your-duo")
                    .WithDescription("Who you will be duoing")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.String))
                    .AddOption(new SlashCommandOptionBuilder().WithName("your-duos-role")
                    .WithDescription("What role you will be Queueing as")
                    .WithRequired(true)
                    .AddChoice("Top", 0)
                    .AddChoice("Jungle", 1)
                    .AddChoice("Mid", 2)
                    .AddChoice("Bottom", 3)
                    .AddChoice("Support", 4)
                    .WithType(ApplicationCommandOptionType.Integer));
            var removeFromQueueCommand = new SlashCommandBuilder()
                .WithName("remove-player")
                .WithDescription("Remove a player from Queue (Admin Command)")
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .AddOption(new SlashCommandOptionBuilder().WithName("user")
                .WithDescription("User to remove from Queue")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.String)
                );
            var clearQueueCommand = new SlashCommandBuilder()
                .WithName("clear-queue")
                .WithDescription("Clear queue (Admin Command)")
                .WithDefaultMemberPermissions(GuildPermission.Administrator);

            try
            {

                await _client.CreateGlobalApplicationCommandAsync(duoCommand.Build());
                await _client.CreateGlobalApplicationCommandAsync(removeFromQueueCommand.Build());
                await _client.CreateGlobalApplicationCommandAsync(clearQueueCommand.Build());
            }
            catch (ApplicationCommandException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                Console.WriteLine(json);
            }
        }

        private static async Task ButtonHandler(SocketMessageComponent component)
        {
            await component.DeferAsync();
            switch (component.Data.CustomId)
            {
                case "queue-top":
                    await _handler.QueuePlayer(QueueHandler.Role.Top, component.User);
                    break;
                case "queue-jungle":
                    await _handler.QueuePlayer(QueueHandler.Role.Jungle, component.User);
                    break;
                case "queue-mid":
                    await _handler.QueuePlayer(QueueHandler.Role.Mid, component.User);
                    break;
                case "queue-bottom":
                    await _handler.QueuePlayer(QueueHandler.Role.Bottom, component.User);
                    break;
                case "queue-support":
                    await _handler.QueuePlayer(QueueHandler.Role.Support, component.User);
                    break;
                case "queue-leave":
                    await _handler.LeaveQueue(component.User);
                    break;
                case "duo-accept":
                    await HandleDuoAccept(component);
                    break;

            }
        }

        public static async Task HandleQueueMessage()
        {
            if (await _channel.GetMessageAsync(_queueMessage.Id) == null) { return; }

            string queueMessage = $"```" +
                $"Top:     {_handler.queue.Top.Count} {string.Join(", ", _handler.queue.Top.Select(player => _client.GetUser(player.DiscordID)))}\n" +
                $"Jungle:  {_handler.queue.Jungle.Count} {string.Join(", ", _handler.queue.Jungle.Select(player => _client.GetUser(player.DiscordID)))}\n" +
                $"Mid:     {_handler.queue.Mid.Count} {string.Join(", ", _handler.queue.Mid.Select(player => _client.GetUser(player.DiscordID)))}\n" +
                $"Bottom:  {_handler.queue.Bottom.Count} {string.Join(", ", _handler.queue.Bottom.Select(player => _client.GetUser(player.DiscordID)))}\n" +
                $"Support: {_handler.queue.Support.Count} {string.Join(", ", _handler.queue.Support.Select(player => _client.GetUser(player.DiscordID)))}\n" +
                $"--------------------------------------------```";

            string duoMessage = _handler.queue.Duos.Count == 0 ? 
                "```Duos will appear here```" 
                : 
                $"```{string.Join("\n", _handler.queue.Duos.Select(players => $"{_client.GetUser(players.Item1.DiscordID)} & {_client.GetUser(players.Item2.DiscordID)}"))}```";

            var embed = new EmbedBuilder
            {
                Title = $"[{_handler.queue.AllRoles.SelectMany(list => list).Count()} Summoner(s) in Queue]"
            };
            embed.AddField("Queue", queueMessage, false);
            embed.AddField("Duos", duoMessage, false);

            await _queueMessage.ModifyAsync(msg => msg.Embed = embed.Build());
        }
    }
}
