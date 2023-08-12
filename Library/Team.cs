using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PROJECT_g0la
{
    public class Team
    {
        public Player? Top { get; set; }
        public Player? Jungle { get; set; }
        public Player? Mid { get; set; }
        public Player? Bottom { get; set; }
        public Player? Support { get; set; }
        public List<Player> AllPlayers = new();
        public QueueHandler.Side Side { get; set; }

        public Team(QueueHandler.Side side)
        {
            Side = side;
        }
    }
}
