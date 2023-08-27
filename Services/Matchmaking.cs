using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PROJECT_g0la
{
    internal class Matchmaking
    {
        public static List<Team> MakeTeamsMoreEven(List<Team> teams, int depth)
        {
            List<Team> bestSolution = CreateDeepCopy(teams);
            int difference = GetDifference(bestSolution);
            int bestDifference = GetDifference(bestSolution);

            for (int i = 0; i < teams[0].AllPlayers.Count; i++)
            {
                List<Team> tempList = CreateDeepCopy(teams);

                Player temp1 = tempList[0].AllPlayers[i];
                Player temp2 = tempList[1].AllPlayers[i];
                tempList[0].AllPlayers[i] = tempList[1].AllPlayers[i];
                tempList[1].AllPlayers[i] = temp1;

                if (temp1.Duo is not null)
                {
                    for (int b = 0; b < teams[0].AllPlayers.Count; b++)
                    {
                        if (tempList[0].AllPlayers[b] == temp1.Duo)
                        {
                            Player temp = tempList[0].AllPlayers[b];
                            tempList[0].AllPlayers[b] = tempList[1].AllPlayers[b];
                            tempList[1].AllPlayers[b] = temp;
                        }
                    }
                }
                if (temp2.Duo is not null)
                {
                    for (int b = 0; b < teams[0].AllPlayers.Count; b++)
                    {
                        if (tempList[1].AllPlayers[b] == temp2.Duo)
                        {
                            Player temp = tempList[1].AllPlayers[b];
                            tempList[1].AllPlayers[b] = tempList[0].AllPlayers[b];
                            tempList[0].AllPlayers[b] = temp;
                        }
                    }
                }


                int newDifference = GetDifference(tempList);

                if (newDifference < bestDifference)
                {
                    bestSolution = CreateDeepCopy(tempList);
                    bestDifference = newDifference;
                }

            }


            if (depth == 0 || GetDifference(bestSolution) == difference)
            {
                return bestSolution;
            }
            else
            {
                return MakeTeamsMoreEven(bestSolution, depth - 1);
            }

        }

        static List<Team> CreateDeepCopy(List<Team> teams)
        {
            List<Team> copiedList = new();

            foreach (Team team in teams)
            {
                Team tempTeam = new Team(team.Side);

                foreach (Player player in team.AllPlayers)
                {
                    tempTeam.AllPlayers.Add(player);
                }

                copiedList.Add(tempTeam);
            }

            return copiedList;
        }

        static int GetDifference(List<Team> teams)
        {
            return (int)Math.Abs(teams[0].AverageElo - teams[1].AverageElo);
        }
    }
}
