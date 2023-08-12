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

        public QueueObject()
        {
            AllRoles.Add(Top);
            AllRoles.Add(Jungle);
            AllRoles.Add(Mid);
            AllRoles.Add(Bottom);
            AllRoles.Add(Support);
        }
        public async Task EnterQueue(Player player, QueueHandler.Role role)
        {
            switch (role)
            {
                case Role.Top:
                    await QueuePlayer(player, Top);
                    break;
                case Role.Jungle:
                    await QueuePlayer(player, Jungle);
                    break;
                case Role.Mid:
                    await QueuePlayer(player, Mid);
                    break;
                case Role.Bottom:
                    await QueuePlayer(player, Bottom);
                    break;
                case Role.Support:
                    await QueuePlayer(player, Support);
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
        }

        private async Task CheckQueue()
        {
            bool isPop = AllRoles.Any(list => list.Count >= 2);
            if (isPop)
            {
                await _handler.QueuePop();
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

        private async Task QueuePlayer(Player player, List<Player> roleList)
        {
            if (player.Duo is not null) { await QueueDuoPlayer(player, roleList); return; }
            await player.RemoveQueue();
            roleList.Add(player);
            player.QueuePosition = roleList;
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
