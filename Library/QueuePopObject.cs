using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PROJECT_g0la.Library
{
    public class QueuePopObject
    {
        public List<Player> Top = new();
        public List<Player> Jungle = new();
        public List<Player> Mid = new();
        public List<Player> Bottom = new();
        public List<Player> Support = new();

        public List<List<Player>> AllRoles = new();

        public List<Player> AllPlayers = new();

        public bool Accepted = false;

        public QueuePopObject()
        {
            AllRoles.Add(Top);
            AllRoles.Add(Jungle);
            AllRoles.Add(Mid);
            AllRoles.Add(Bottom);
            AllRoles.Add(Support);
        }
    }
}
