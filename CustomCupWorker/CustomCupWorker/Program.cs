using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using MySql.Data.MySqlClient;

namespace CustomCupWorker
{
    class MainClass
    {
        const string ConnectionString = "server=localhost;user=root;password=;database=kTVCSS;";

        private static string GetSteams(string teamA, string teamB)
        {
            var stuff = "";
            var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            var query = new MySqlCommand($"SELECT TSTUFF FROM ccw_teams WHERE TNAME = '{teamA}' OR TNAME = '{teamB}'", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                stuff += reader[0].ToString();
            }
            connection.Close();
            return stuff;
        }

        private static void Kicker(object args)
        {
            var input = args.ToString().Split(';');
            string tTrueName = input[0];
            string ctTrueName = input[1];
            string host = input[2];
            string port = input[3];
            string password = input[4];
            DateTime end = DateTime.Parse(input[5]);
            string id = input[6];

            var webClient = new WebClient();

            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            while (true)
            {
                var stuffFromDb = GetSteams(tTrueName, ctTrueName);

                var stuffFromServer = webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host + "&port=" + port + "&password=" + password + "&command=sm_kurwagay").Split('\n');

                stuffFromServer[0] = stuffFromServer[0].Substring(stuffFromServer[0].IndexOf("\"") + 1);

                foreach (var item in stuffFromServer)
                {
                    if (!item.Contains("STEAM")) continue;
                    var steam = item.Split(';');
                    if (!stuffFromDb.Contains(steam[1]))
                    {
                        webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host +
                        "&port=" + port +
                            "&password=" + password +
                        "&command=kickid " + steam[0] + " [kTVCSSCupBot] Access on this match is denied for you");
                        Console.WriteLine(DateTime.Now + ": кикаем " + steam[1] + " с сервера " + host + ":" + port);
                    }
                }

                if (end.Day == DateTime.Now.Day && end.Hour == DateTime.Now.Hour && end.Minute == DateTime.Now.Minute)
                {
                    Console.WriteLine(DateTime.Now + ": завершаем матч " + tTrueName + " против " + ctTrueName);
                    DeleteMatch(id);
                    return;
                }

                Thread.Sleep(30000);
            }
        }

        public static void UpdateMatchStatus(string id)
        {
            var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            var query = new MySqlCommand($"UPDATE ccw_matches SET playing = 1 WHERE Id = {id}", connection);
            query.ExecuteNonQuery();
            connection.Close();
        }

        public static void DeleteMatch(string id)
        {
            var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            var query = new MySqlCommand($"DELETE FROM ccw_matches WHERE Id = {id}", connection);
            query.ExecuteNonQuery();
            connection.Close();
        }

        public static string[] GetServerInfo(string id)
        {
            List<string> result = new List<string>();
            var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            var query = new MySqlCommand($"SELECT Host, GamePort, RconPassword FROM servers WHERE Id = {id}", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader[0].ToString());
                result.Add(reader[1].ToString());
                result.Add(reader[2].ToString());
            }
            connection.Close();
            return result.ToArray();
        }

        public static void Main(string[] args)
        {
            Console.Title = "Custom Cup Worker";
            Console.ForegroundColor = ConsoleColor.Green;
            // for program reloading
            var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            var query = new MySqlCommand($"SELECT TeamAName, TeamBName, ServerId, DateTimeStart, DateTimeEnd, Id FROM ccw_matches WHERE playing = 1", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                var teamA = reader[0].ToString();
                var teamB = reader[1].ToString();
                var sId = reader[2].ToString();
                var ds = DateTime.Parse(reader[3].ToString());
                var de = DateTime.Parse(reader[4].ToString());
                var id = reader[5].ToString();
                //UpdateMatchStatus(id);
                Console.WriteLine(DateTime.Now + ": обновляем статус матча " + id);
                var server = GetServerInfo(sId);
                Thread thread = new Thread(Kicker);
                thread.Start(teamA + ";" + teamB + ";" + server[0] + ";" + server[1] + ";" + server[2] + ";" + de + ";" + id);
                Console.WriteLine(DateTime.Now + ": запустили поток на обработку");
            }
            connection.Close();
            // main proc
            while (true)
            {
                connection = new MySqlConnection(ConnectionString);
                connection.Open();
                query = new MySqlCommand($"SELECT TeamAName, TeamBName, ServerId, DateTimeStart, DateTimeEnd, Id FROM ccw_matches WHERE playing = 0", connection);
                reader = query.ExecuteReader();
                while (reader.Read())
                {
                    var teamA = reader[0].ToString();
                    var teamB = reader[1].ToString();
                    var sId = reader[2].ToString();
                    var ds = DateTime.Parse(reader[3].ToString());
                    var de = DateTime.Parse(reader[4].ToString());
                    var id = reader[5].ToString();
                    if (ds.Day == DateTime.Now.Day && ds.Hour == DateTime.Now.Hour && ds.Minute == DateTime.Now.Minute)
                    {
                        UpdateMatchStatus(id);
                        Console.WriteLine(DateTime.Now + ": обновляем статус матча " + id);
                        var server = GetServerInfo(sId);
                        Thread thread = new Thread(Kicker);
                        thread.Start(teamA + ";" + teamB + ";" + server[0] + ";" + server[1] + ";" + server[2] + ";" + de + ";" + id);
                        Console.WriteLine(DateTime.Now + ": запустили поток на обработку");
                    }
                }
                connection.Close();
                Thread.Sleep(30000);
            }
        }
    }
}
