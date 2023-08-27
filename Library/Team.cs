using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

        public void LoadPlayers()
        {
            for (int i = 0; i < AllPlayers.Count; i++)
            {
                switch (i)
                {
                    case 0:
                        Top = AllPlayers[i];
                        break;
                    case 1:
                        Jungle = AllPlayers[i];
                        break;
                    case 2:
                        Mid = AllPlayers[i];
                        break;
                    case 3:
                        Bottom = AllPlayers[i];
                        break;
                    case 4:
                        Support = AllPlayers[i];
                        break;

                }
            }
        }

        public double AverageElo => AllPlayers.Average(player => player.Elo);
    }
}
