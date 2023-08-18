using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static PROJECT_g0la.QueueHandler;

namespace PROJECT_g0la
{
    public class QueueObject
    {
        public List<Player> Top = new();
        public List<Player> Jungle = new();
        public List<Player> Mid = new();
        public List<Player> Bottom = new();
        public List<Player> Support = new();

        public List<List<Player>> AllRoles = new();

        public List<Tuple<Player, Player>> Duos = new();

        public QueueHandler _handler { get; set; }

        public enum QueueEvent
        {
            Enter,
            EnterDuo,
            Leave,
            Pop,
            Accepted,
            none
        }

        public QueueObject()
        {
            AllRoles.Add(Top);
            AllRoles.Add(Jungle);
            AllRoles.Add(Mid);
            AllRoles.Add(Bottom);
            AllRoles.Add(Support);
        }

        public async Task LogQueueEvents(Role role, QueueEvent queueEvent, Tuple<Tuple<Player, Role>, Tuple<Player, Role>> duo = null, Player player = null)
        {
            switch (queueEvent)
            {
                case QueueEvent.Enter:
                    await Program.Log(new LogMessage(LogSeverity.Info, "QueueLog", $"{Program._client.GetUser(player.DiscordID).Username} entered Queue for role {role}"));
                    break;
                case QueueEvent.EnterDuo:
                    await Program.Log(new LogMessage(LogSeverity.Info, "QueueLog", $"{Program._client.GetUser(duo.Item1.Item1.DiscordID).Username} entered Queue for role {duo.Item1.Item2} with Duo <{Program._client.GetUser(duo.Item2.Item1.DiscordID).Username}> for role {duo.Item2.Item2}"));
                    break;
                case QueueEvent.Leave:
                    await Program.Log(new LogMessage(LogSeverity.Info, "QueueLog", $"{Program._client.GetUser(player.DiscordID).Username} left Queue"));
                    break;
                case QueueEvent.Pop:
                    await Program.Log(new LogMessage(LogSeverity.Info, "QueueLog", $"Queue popped"));
                    break;
                case QueueEvent.Accepted:
                    await Program.Log(new LogMessage(LogSeverity.Info, "QueueLog", $"Queue accepted"));
                    break;
            }
         }

        public async Task EnterQueue(Player player, QueueHandler.Role role)
        {
            switch (role)
            {
                case Role.Top:
                    await QueuePlayer(player, Top, role);
                    break;
                case Role.Jungle:
                    await QueuePlayer(player, Jungle, role);
                    break;
                case Role.Mid:
                    await QueuePlayer(player, Mid, role);
                    break;
                case Role.Bottom:
                    await QueuePlayer(player, Bottom, role);
                    break;
                case Role.Support:
                    await QueuePlayer(player, Support, role);
                    break;
            }

            //await PrintQueue();
            await MessageHandler.HandleQueueMessage();
            await CheckQueue();
        }

        public async Task LeaveQueue(Player player)
        {
            await player.RemoveQueue();
            await MessageHandler.HandleQueueMessage();

            await LogQueueEvents(Role.Mid, QueueEvent.Leave, player:player);
        }

        private async Task CheckQueue()
        {
            bool isPop = AllRoles.Any(list => list.Count >= 2);
            if (isPop)
            {
                await _handler.QueuePop();
                await LogQueueEvents(Role.Mid, QueueEvent.Pop);
            }
        }

        private async Task PrintQueue()
        {
            Console.Clear();
            Console.WriteLine("TOP: [" + string.Join(",", Top) + "]");
            Console.WriteLine("JUNGLE: [" + string.Join(",", Jungle) + "]");
            Console.WriteLine("MID: [" + string.Join(",", Mid) + "]");
            Console.WriteLine("BOTTOM: [" + string.Join(",", Bottom) + "]");
            Console.WriteLine("SUPPORT: [" + string.Join(",", Support) + "]");
        }

        private async Task QueuePlayer(Player player, List<Player> roleList, Role role)
        {
            if (player.Duo is not null) { await QueueDuoPlayer(player, roleList); return; }
            await player.RemoveQueue();
            roleList.Add(player);
            player.QueuePosition = roleList;

            await LogQueueEvents(role, QueueEvent.Enter, player:player);
        }

        private async Task QueueDuoPlayer(Player player, List<Player> roleList)
        {
            await player.RemoveQueueTemp();
            roleList.Add(player);
            player.QueuePosition = roleList;
        }

        public async Task ClearQueue()
        {
            foreach(List<Player> roleList in AllRoles)
            {
                foreach(Player player in new List<Player>(roleList))
                {
                    await player.RemoveQueue();
                }
            }
            await MessageHandler.HandleQueueMessage();
        }
    }

    
}
