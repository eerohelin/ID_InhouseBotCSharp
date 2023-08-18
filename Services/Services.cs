using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PROJECT_g0la
{
    public class Services
    {

        public static class MockData
        {
            public static List<Player> Top = new()
            {
                new Player() { Summoner = "h0laa", DiscordID = 100},
                new Player() { Summoner = "h0laa", DiscordID = 100}
            };
            public static List<Player> Jungle = new()
            {
                new Player() { Summoner = "BL Bitse", DiscordID = 100},
                new Player() { Summoner = "BL Bitse", DiscordID = 100}
            };
            public static List<Player> Mid = new()
            {
                new Player() { Summoner = "Ozziee", DiscordID = 100},
                new Player() { Summoner = "Ozziee", DiscordID = 100}
            };
            public static List<Player> Bottom = new()
            {
                new Player() { Summoner = "PeaceRuler", DiscordID = 100},
                new Player() { Summoner = "PeaceRuler", DiscordID = 100}
            };
            public static List<Player> Support = new()
            {
                new Player() { Summoner = "Pluffuff", DiscordID = 100},
                new Player() { Summoner = "Pluffuff", DiscordID = 100}
            };

            public static List<List<Player>> AllRoles = new()
            {
                Top, Jungle, Mid, Bottom, Support
            };

            public static async Task AssignDuos()
            {
                Top[1].Duo = Jungle.First();
                Jungle.First().Duo = Top[1];
            }

        };
        public static async Task<Player> GetPlayerFromDB(ulong ID)
        {
            string filePath = "database.json";
            string jsonContent = System.IO.File.ReadAllText(filePath);

            PlayersData playersData = JsonConvert.DeserializeObject<PlayersData>(jsonContent);
            try
            {
                Player player = playersData.Players[ID.ToString()];
                player.DiscordID = ID;

                return player;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static async Task<Config> GetConfig()
        {
            string filePath = "config.json";
            try
            {
                string jsonContent = System.IO.File.ReadAllText(filePath);
                Config config = JsonConvert.DeserializeObject<Config>(jsonContent);

                return config;
            } catch (FileNotFoundException ex)
            {
                return null;
            }
        }

        public class ROFLHandler
        {
            public static ReplayObject ParseROFL(string path)
            {
                if (!path.Contains(".rofl")) { return null; }
                string replayFileContents = string.Join("", File.ReadLines(path, Encoding.Default).Take(20).ToList<string>().ToArray<string>());
                return GetReplay(replayFileContents);
            }


            private static ReplayObject GetReplay(string replayFileContents)
            {
                int jsonStartIndex = replayFileContents.IndexOf("{\"gameLength\"");
                int jsonEndIndex = replayFileContents.IndexOf("\\\"}]\"}") + "\\\"}]\"}".Length;

                try
                {
                    JsonNode parsed = JsonObject.Parse(replayFileContents.Substring(jsonStartIndex, (jsonEndIndex - jsonStartIndex)));

                    string cleanedJSON = parsed.ToString().Replace("\"[", "[").Replace("]\"", "]").Replace(@"\u0022", "\"");
                    ReplayObject replay = System.Text.Json.JsonSerializer.Deserialize<ReplayObject>(cleanedJSON);
                    return replay;

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return null;
                }
            }
        }
    }

    public class PlayersData
    {
        public Dictionary<string, Player>? Players { get; set; }
    }

    public class ReplayObject
    {
        public int? gameLength { get; set; }
        public string? gameVersion { get; set; }
        public List<Summoner>? statsJson { get; set; }
    }


    public class Summoner
    {
        public string? ASSISTS { get; set; }
        public string? CHAMPIONS_KILLED { get; set; }
        public string? ID { get; set; }
        public string? INDIVIDUAL_POSITION { get; set; }
        public string? LEVEL { get; set; }
        public string? NAME { get; set; }
        public string? NUM_DEATHS { get; set; }
        public string? PLAYER_POSITION { get; set; }
        public string? PLAYER_ROLE { get; set; }
        public string? PUUID { get; set; }
        public string? SKIN { get; set; }
        public string? TEAM { get; set; }
        public string? TEAM_POSITION { get; set; }
        public string? VICTORY_POINT_TOTAL { get; set; }
        public string? WIN { get; set; }
    }


    public class Config
    {
        public string BOT_KEY { get; set; }
        public ulong CHANNEL_QUEUE { get; set; }
        public ulong CATEGORY_GAMES { get; set; }
        public ulong GUILD_ID { get; set; }
    }


}
