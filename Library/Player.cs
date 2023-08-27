using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PROJECT_g0la
{
    public class Player
    {
        public ulong DiscordID { get; set; }
        public string Username => Program._client.GetUser(DiscordID).Username;
        public string? Summoner { get; set; }
        public int? Wins { get; set; }
        public int? Losses { get; set; }
        public int Elo { get; set; }
        public List<Player>? QueuePosition { get; set; }
        public List<Tuple<Player, Player>>? DuoList { get; set; }
        public Player? Duo { get; set; }
        public QueueHandler.Side Side = QueueHandler.Side.None;
        public bool QueueAccepted = false;
        public async Task<bool> RemoveQueue()
        {
            if (QueuePosition is null) return false;

            QueuePosition.Remove(this);
            if (Duo is not null) 
            {
                DuoList.Remove(new Tuple<Player, Player>(this, Duo));
                DuoList.Remove(new Tuple<Player, Player>(Duo, this));
                DuoList = null;
                Duo.Duo = null; 
                await Duo.RemoveQueue(); 
            }
            Duo = null;
            QueuePosition = null;
            return true;
        }

        public async Task RemoveQueueTemp()
        {
            if (QueuePosition is null) return;
            QueuePosition.Remove(this);
            QueuePosition = null;
        }
    }
}
