using Discord;
using Discord.WebSocket;
using PROJECT_g0la.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static PROJECT_g0la.QueueHandler;

namespace PROJECT_g0la
{
    public class QueueHandler
    {
        public Dictionary<string, Player> _players = new();
        public QueueObject queue = new();
        private QueuePopObject? _pop;

        public enum Role
        {
            Top,
            Jungle,
            Mid,
            Bottom,
            Support
        }

        public enum Side
        {
            Blue,
            Red,
            None
        }

        public async Task StartQueue()
        {
            MessageHandler._handler = this;
            await MessageHandler.StartQueue();
            queue._handler = this;
        }

        public async Task QueuePop()
        {
            _pop = new();
            for (int i = 0; i < queue.AllRoles.Count; i++)
            {
                foreach(Player player in queue.AllRoles[i].Take(2))
                {
                    _pop.AllRoles[i].Add(player);
                    _pop.AllPlayers.Add(player);
                }
            }
            await MessageHandler.SendQueuePopMessage(_pop);
        }

        public async Task CheckQueuePop()
        {
            bool allAccepted = _pop.AllPlayers.All(player => player.QueueAccepted);

            if (allAccepted) { await QueuePopWentThroughHandler(); }
        }

        public async Task PlayerAccept(ulong userId)
        {
            if (!_players.ContainsKey(userId.ToString())) { return; }
            Player player = _players[userId.ToString()];
            if (_pop.AllPlayers.Contains(player))
            {
                player.QueueAccepted = true;
            }
            await Program.Log(new LogMessage(LogSeverity.Info, "QueuePop", $"{Program._client.GetUser(player.DiscordID).Username} accepted the QueuePop"));
            await MessageHandler.UpdateQueuePopMessage(_pop);
            await CheckQueuePop();

        }

        public async Task PlayerDecline(ulong id)
        {
            if (!_players.ContainsKey(id.ToString())) { return; }
            await LeaveQueue(Program._client.GetUser(id));
            await MessageHandler.CancelQueuePop(Program._client.GetUser(id));

            await Program.Log(new LogMessage(LogSeverity.Info, "QueuePop", $"{Program._client.GetUser(id).Username} declined Pop"));
        }

        public async Task QueuePopWentThroughHandler()
        {
            if (_pop.Accepted) { return; }
            await queue.LogQueueEvents(Role.Mid, QueueObject.QueueEvent.Accepted);
            _pop.Accepted = true;
            QueuePopSuccessObject queuePop = new();

            // MOCK DATA! REPLACE WITH QUEUEOBJECT ROLES
            await Services.MockData.AssignDuos();
            List<List<Player>> roleList = Services.MockData.AllRoles;

            Team blueTeam = new(Side.Blue);
            Team redTeam = new(Side.Red);


            for (int i = 0; i < roleList.Count; i++) // Get first two players from each role
            {
                List<Player> list = roleList[i];
                List<Player> firstTwo = list.Take(2).ToList();

                await AssignSides(firstTwo);

                foreach (Player player in firstTwo)
                {
                    if (!queuePop.Teams.ContainsKey(player.Side)) { queuePop.Teams[player.Side] = new Team(player.Side); }

                    switch(player.Side)
                    {
                        case Side.Blue:
                            blueTeam.AllPlayers.Add(player);
                            break;
                        case Side.Red:
                            redTeam.AllPlayers.Add(player);
                            break;
                    }

                    // Reset Player properties
                    player.QueueAccepted = false;
                    player.Side = Side.None;
                    await player.RemoveQueue();
                }
            }

            List<Team> teams = Matchmaking.MakeTeamsMoreEven(new List<Team>() { blueTeam, redTeam }, 3);

            queuePop.Teams[Side.Blue] = teams[0];
            queuePop.Teams[Side.Red] = teams[1];

            queuePop.Teams[Side.Blue].LoadPlayers();
            queuePop.Teams[Side.Red].LoadPlayers();

            foreach (List<Player> list in roleList)
            {
                list.RemoveRange(0, 2);
            }

            await MessageHandler.HandleQueuePopAccept(queuePop);
        }

        public async Task AssignSides(List<Player> players)
        {
            List<Side> sides = new List<Side>() { Side.Blue, Side.Red };
            foreach (Player player in players)
            {
                if (player.Side is not Side.None) { sides.Remove(player.Side); continue; }
                Random random = new();
                int randomNum = random.Next(sides.Count);
                player.Side = sides[randomNum];
                if (player.Duo is not null) { player.Duo.Side = sides[randomNum]; }
                sides.RemoveAt(randomNum);
            }
        }

        public async Task QueuePlayer(Role role, SocketUser user)
        {
            Player player = await CheckPlayerDB(user);

            await queue.EnterQueue(player, role);
        }

        public async Task LeaveQueue(SocketUser user)
        {
            await queue.LeaveQueue(_players[user.Id.ToString()]);
        }

        public async Task QueueDuo(Tuple<Tuple<SocketUser, Role>, Tuple<SocketUser, Role>> playerList)
        {
            Tuple<SocketUser, Role> userTuple1 = playerList.Item1;
            Tuple<SocketUser, Role> userTuple2 = playerList.Item2;

            Player player1 = CheckPlayerDB(userTuple1.Item1).Result;
            Player player2 = CheckPlayerDB(userTuple2.Item1).Result;

            player1.Duo = player2;
            player2.Duo = player1;

            player1.DuoList = queue.Duos;
            player2.DuoList = queue.Duos;

            queue.Duos.Add(new Tuple<Player, Player>(player1, player2));

            await queue.EnterQueue(player1, userTuple1.Item2);
            await queue.EnterQueue(player2, userTuple2.Item2);

            Tuple<Tuple<Player, Role>, Tuple<Player, Role>> duoTuple = new Tuple<Tuple<Player, Role>, Tuple<Player, Role>>
            (
                new Tuple<Player, Role>(player1, userTuple1.Item2),
                new Tuple<Player, Role>(player2, userTuple2.Item2)
            );
            await queue.LogQueueEvents(Role.Mid, QueueObject.QueueEvent.EnterDuo, duo: duoTuple);
        }

        public async Task<Player> CheckPlayerDB(SocketUser user)
        {
            Player player = new();
            if (_players.ContainsKey(user.Id.ToString()))
            {
                player = _players[user.Id.ToString()];
            }
            else
            {
                Task<Player> getPlayer = Services.GetPlayerFromDB(user.Id);
                if (getPlayer != null) { player = getPlayer.Result; _players.Add(user.Id.ToString(), getPlayer.Result); }
            }
            return player;
        }
    }
}
