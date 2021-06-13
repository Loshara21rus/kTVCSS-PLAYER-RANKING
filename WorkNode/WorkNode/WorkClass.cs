using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using VkNet;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

namespace WorkNode
{
    internal static class WorkClass
    {
        #region Global vars

        public static string ProgramName = "KPR 2.0.0";
        private static List<string> _log = new List<string>();
        public static string ConnectionString;
        private static List<string> _playerInUse = new List<string>();
        private static List<Players> _playerList = new List<Players>();
        public static List<Servers> ServerList = new List<Servers>();
        static List<string> _playerStatsAfterMatch = new List<string>();
        public static string Token = "null";
        public static int MainGroupId = 0;
        public static int StatGroupId = 0;
        public static int AdminUserId = 0;

        #endregion

        public struct Servers
        {
            public string Host;
            public string UserName;
            public string UserPassword;
            public int Port;
            public string LogsDir;
            public int GamePort;
            public string RconPassword;

            public Servers(string host, string userName, string userPassword, int port, string logsDir, int gamePort, string rconPassword)
            {
                this.Host = host;
                this.UserName = userName;
                this.UserPassword = userPassword;
                this.Port = port;
                this.LogsDir = logsDir;
                this.GamePort = gamePort;
                this.RconPassword = rconPassword;
            }
        }

        private struct Players
        {
            public string PlayerName;
            public string PlayerSteamId;
            public int PlayerKill;
            public int PlayerDeath;
            public int PlayerRank;
            public int PlayerHeadshot;
            public string PlayerTeam;

            public Players(string playerName, string playerSteamId, int playerKill, int playerDeath, int playerRank, int playerHeadshot, string playerTeam, bool playerVictory)
            {
                this.PlayerName = playerName;
                this.PlayerSteamId = playerSteamId;
                this.PlayerKill = playerKill;
                this.PlayerDeath = playerDeath;
                this.PlayerRank = playerRank;
                this.PlayerHeadshot = playerHeadshot;
                this.PlayerTeam = playerTeam;
            }
        }

        private static void MultiCountProcessing(string filePath)
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            foreach (var line in lines)
            {
                if (!line.Contains("killed")) continue;
                var playersIds = new List<string>();
                var playersNames = new List<string>();
                var steamIds = new List<string>();
                var playersTeams = new List<string>();
                var splitted = Regex.Split(line, @".killed.""");
                foreach (var split in splitted)
                {
                    var playerIdRe = Regex.Matches(split, @"\<\d+\>");
                    var idCount = playerIdRe.Count - 1;
                    var playerId = playerIdRe[idCount].ToString();
                    var nameSplitter = playerId;
                    playerId = Regex.Replace(playerId, @"[\<\>]", "");
                    playersIds.Add(playerId);

                    var playerName = Regex.Split(split, nameSplitter)[0];
                    playerName = Regex.Replace(playerName, @".+?""", "");
                    playersNames.Add(playerName);

                    var steamIdRe = Regex.Matches(split, @"\<STEAM_.+?\>");
                    idCount = steamIdRe.Count - 1;
                    var steamId = steamIdRe[idCount].ToString();
                    steamId = Regex.Replace(steamId, @"[\<\>]", "");
                    steamIds.Add(steamId);

                    var playersTeamsRe = Regex.Matches(split, @"\<[A-Z]+\>");
                    idCount = playersTeamsRe.Count - 1;
                    var playerTeamIs = playersTeamsRe[idCount].ToString();
                    playerTeamIs = Regex.Replace(playerTeamIs, @"[\<\>]", "");
                    playerTeamIs = playerTeamIs.Contains("TERROR") ? "T" : "CT";
                    playersTeams.Add(playerTeamIs);
                }

                if (!_playerInUse.Contains(steamIds[0]))
                {
                    _playerInUse.Add(steamIds[0]);
                    var player = new Players
                    {
                        PlayerName = playersNames[0].Replace("'", null),
                        PlayerSteamId = steamIds[0],
                        PlayerTeam = playersTeams[0],
                        PlayerKill = 1
                    };
                    if (line.Contains("(headshot)"))
                    {
                        player.PlayerHeadshot = 1;
                    }

                    _playerList.Add(player);
                }
                else
                {
                    if (!TeamKillCheck(line))
                    {
                        for (var k = 0; k < _playerList.Count(); k++)
                        {
                            if (_playerList[k].PlayerSteamId != steamIds[0]) continue;
                            var thisPlayer = _playerList[k];
                            thisPlayer.PlayerKill += 1;
                            thisPlayer.PlayerTeam = playersTeams[0];
                            if (line.Contains("(headshot)"))
                            {
                                thisPlayer.PlayerHeadshot += 1;
                            }

                            _playerList[k] = thisPlayer;
                        }
                    }
                }

                for (var k = 0; k < _playerList.Count(); k++)
                {
                    if (_playerList[k].PlayerSteamId != steamIds[1]) continue;
                    if (!TeamKillCheck(line))
                    {
                        var thisPlayer = _playerList[k];
                        thisPlayer.PlayerDeath += 1;
                        thisPlayer.PlayerTeam = playersTeams[1];
                        _playerList[k] = thisPlayer;
                    }
                }
            }
        }

        private static bool TeamKillCheck(string line)
        {
            try
            {
                var playersTeams = Regex.Matches(line, @"\<[A-Z]+\>");
                if (playersTeams.Count != 2) return false;
                if (playersTeams[0] == playersTeams[1])
                    return true;
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static int WinnerDefine(string filePath)
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            foreach (var line in lines)
            {
                if (line.Contains("WarMod triggered \"full_time\""))
                {
                    var input = line.Substring(line.IndexOf("full_time", StringComparison.Ordinal) + 11);
                    input = input.Replace("\"", "");
                    var result = input.Split(' ');
                    if (result[0] != "Terrorists") result[0] = "Terrorists";
                    if (result[2] != "Counter-Terrorists") result[0] = "Counter-Terrorists";
                    return int.Parse(result[1]) > int.Parse(result[3]) ? 0 : 1;
                }
                if (line.Contains("WarMod triggered \"over_full_time\""))
                {
                    var input = line.Substring(line.IndexOf("over_full_time", StringComparison.Ordinal) + 16);
                    input = input.Replace("\"", "");
                    var result = input.Split(' ');
                    if (result[0] != "Terrorists") result[0] = "Terrorists";
                    if (result[2] != "Counter-Terrorists") result[0] = "Counter-Terrorists";
                    return int.Parse(result[1]) > int.Parse(result[3]) ? 0 : 1;
                }
            }
            return 2;
        }

        private static string[] GetMatchResults(string filePath)
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            foreach (var line in lines)
            {
                if (line.Contains("WarMod triggered \"full_time\""))
                {
                    var input = line.Substring(line.IndexOf("full_time", StringComparison.Ordinal) + 11);
                    input = input.Replace("\"", "");
                    var result = input.Split(' ');
                    return result;
                }
                if (line.Contains("WarMod triggered \"over_full_time\""))
                {
                    var input = line.Substring(line.IndexOf("over_full_time", StringComparison.Ordinal) + 16);
                    input = input.Replace("\"", "");
                    var result = input.Split(' ');
                    return result;
                }
            }
            return null;
        }

        private static void GetPlayerResult(string nickName, string kills, string deaths, string kd, string hk)
        {
            if (string.IsNullOrEmpty(nickName)) nickName = " ";
            if (string.IsNullOrEmpty(kills)) kills = "0";
            if (string.IsNullOrEmpty(deaths)) deaths = "0";
            if (string.IsNullOrEmpty(kd)) kd = "0";
            if (string.IsNullOrEmpty(hk)) hk = "0";
            _playerStatsAfterMatch.Add(nickName + "\t" + kills + "\t" + deaths + "\t" + kd + "\t" + hk);
        }

        public static bool IsNormalMatch(string team, int pts)
        {
            var ePts = 0d;
            if (team == "CT")
            {
                var enemies = _playerList.Select(x => x).Where(x => x.PlayerTeam == "T");
                foreach (var enemy in enemies)
                {
                    ePts += Tools.GetPlayerPts(enemy.PlayerSteamId);
                }
                ePts /= Convert.ToDouble(enemies.Count());
                if (pts - ePts <= -700)
                    return true;
                if (ePts - pts <= 700 && ePts - pts >= -700)
                    return true;
                else return false;
            }
            if (team == "T")
            {
                var enemies = _playerList.Select(x => x).Where(x => x.PlayerTeam == "CT");
                foreach (var enemy in enemies)
                {
                    ePts += Tools.GetPlayerPts(enemy.PlayerSteamId);
                }
                ePts /= Convert.ToDouble(enemies.Count());
                if (pts - ePts <= -700)
                    return true;
                if (ePts - pts <= 700 && ePts - pts >= -700)
                    return true;
                else return false;
            }
            return false;
        }

        private static void UpdatePlayerStats(string filePath)
        {
            foreach (var player in _playerList)
            {
                var connection = new MySqlConnection(ConnectionString);
                var connectionReserved = new MySqlConnection(ConnectionString);
                connection.Open();
                var query = new MySqlCommand("SELECT * FROM kTVCSS.players WHERE SteamID = '" + player.PlayerSteamId + "';", connection);
                var reader = query.ExecuteReader();
                var kd = player.PlayerKill / (double)player.PlayerDeath;
                kd = Math.Round(kd, 2);
                var hk = player.PlayerHeadshot / (double)player.PlayerKill;
                hk = Math.Round(hk, 2);
                var tempHk = hk;
                var tempPlayer = player;
                var thisPlayer = tempPlayer;
                thisPlayer.PlayerName = thisPlayer.PlayerName.Replace("/", null);
                thisPlayer.PlayerName = thisPlayer.PlayerName.Replace(@"\", null);
                reader.Read();
                if (reader.HasRows == false)
                {
                    reader.Close();
                    if (WinnerDefine(filePath) == 0)
                    {
                        if (player.PlayerTeam == "T")
                        {
                            var bonusPts = 30;
                            var kdTemp = player.PlayerKill / (double)player.PlayerDeath;
                            kdTemp = Math.Round(kdTemp, 2);
                            query.CommandText = "INSERT INTO kTVCSS.players (`Name`, `SteamID`, `Kills`, `Deaths`, `Headshots`, `KD`, `HK`, `RankPTS`, `MatchesVictories`, `LastMatch`) VALUES ('" + thisPlayer.PlayerName + "', '" + player.PlayerSteamId + "', '" + player.PlayerKill + "', '" + player.PlayerDeath + "', '" + player.PlayerHeadshot + "', '" + kd + "', '" + hk + "', '" + bonusPts + "', '" + "1" + "', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "');";
                            query.ExecuteNonQuery();
                            GetPlayerResult(thisPlayer.PlayerName.Replace("\t", ""), player.PlayerKill.ToString().Replace("\t", ""), player.PlayerDeath.ToString().Replace("\t", ""), kdTemp.ToString().Replace("\t", ""), tempHk.ToString().Replace("\t", ""));
                            SendStatsToUser(player.PlayerSteamId, "Победа", player.PlayerKill.ToString(), player.PlayerDeath.ToString(), player.PlayerHeadshot.ToString(), kdTemp.ToString(), Math.Round((tempHk * 100), 2).ToString() + "%");
                        }
                        else
                        {
                            var bonusPts = -30;
                            var kdTemp = player.PlayerKill / (double)player.PlayerDeath;
                            kdTemp = Math.Round(kdTemp, 2);
                            query.CommandText = "INSERT INTO kTVCSS.players (`Name`, `SteamID`, `Kills`, `Deaths`, `Headshots`, `KD`, `HK`, `RankPTS`, `MatchesDefeats`, `LastMatch`) VALUES ('" + thisPlayer.PlayerName + "', '" + player.PlayerSteamId + "', '" + player.PlayerKill + "', '" + player.PlayerDeath + "', '" + player.PlayerHeadshot + "', '" + kd + "', '" + hk + "', '" + bonusPts + "', '" + "1" + "', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "');";
                            query.ExecuteNonQuery();
                            GetPlayerResult(thisPlayer.PlayerName.Replace("\t", ""), player.PlayerKill.ToString().Replace("\t", ""), player.PlayerDeath.ToString().Replace("\t", ""), kdTemp.ToString().Replace("\t", ""), tempHk.ToString().Replace("\t", ""));
                            SendStatsToUser(player.PlayerSteamId, "Поражение", player.PlayerKill.ToString(), player.PlayerDeath.ToString(), player.PlayerHeadshot.ToString(), kdTemp.ToString(), Math.Round((tempHk * 100), 2).ToString() + "%");
                        }
                    }
                    if (WinnerDefine(filePath) == 1)
                    {
                        if (player.PlayerTeam == "CT")
                        {
                            var bonusPts = 30;
                            var kdTemp = player.PlayerKill / (double)player.PlayerDeath;
                            kdTemp = Math.Round(kdTemp, 2);
                            query.CommandText = "INSERT INTO kTVCSS.players (`Name`, `SteamID`, `Kills`, `Deaths`, `Headshots`, `KD`, `HK`, `RankPTS`, `MatchesVictories`, `LastMatch`) VALUES ('" + thisPlayer.PlayerName + "', '" + player.PlayerSteamId + "', '" + player.PlayerKill + "', '" + player.PlayerDeath + "', '" + player.PlayerHeadshot + "', '" + kd + "', '" + hk + "', '" + bonusPts + "', '" + "1" + "', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "');";
                            query.ExecuteNonQuery();
                            GetPlayerResult(thisPlayer.PlayerName.Replace("\t", ""), player.PlayerKill.ToString().Replace("\t", ""), player.PlayerDeath.ToString().Replace("\t", ""), kdTemp.ToString().Replace("\t", ""), tempHk.ToString().Replace("\t", ""));
                            SendStatsToUser(player.PlayerSteamId, "Победа", player.PlayerKill.ToString(), player.PlayerDeath.ToString(), player.PlayerHeadshot.ToString(), kdTemp.ToString(), Math.Round((tempHk * 100), 2).ToString() + "%");
                        }
                        else
                        {
                            var bonusPts = -30;
                            var kdTemp = player.PlayerKill / (double)player.PlayerDeath;
                            kdTemp = Math.Round(kdTemp, 2);
                            query.CommandText = "INSERT INTO kTVCSS.players (`Name`, `SteamID`, `Kills`, `Deaths`, `Headshots`, `KD`, `HK`, `RankPTS`, `MatchesDefeats`, `LastMatch`) VALUES ('" + thisPlayer.PlayerName + "', '" + player.PlayerSteamId + "', '" + player.PlayerKill + "', '" + player.PlayerDeath + "', '" + player.PlayerHeadshot + "', '" + kd + "', '" + hk + "', '" + bonusPts + "', '" + "1" + "', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "');";
                            query.ExecuteNonQuery();
                            GetPlayerResult(thisPlayer.PlayerName.Replace("\t", ""), player.PlayerKill.ToString().Replace("\t", ""), player.PlayerDeath.ToString().Replace("\t", ""), kdTemp.ToString().Replace("\t", ""), tempHk.ToString().Replace("\t", ""));
                            SendStatsToUser(player.PlayerSteamId, "Поражение", player.PlayerKill.ToString(), player.PlayerDeath.ToString(), player.PlayerHeadshot.ToString(), kdTemp.ToString(), Math.Round((tempHk * 100), 2).ToString() + "%");
                        }
                    }
                    connection.Close();
                    continue;
                }
                thisPlayer.PlayerKill += int.Parse(reader[3].ToString());
                thisPlayer.PlayerDeath += int.Parse(reader[4].ToString());
                thisPlayer.PlayerHeadshot += int.Parse(reader[5].ToString());
                kd = thisPlayer.PlayerKill / (double)thisPlayer.PlayerDeath;
                kd = Math.Round(kd, 2);
                hk = thisPlayer.PlayerHeadshot / (double)thisPlayer.PlayerKill;
                hk = Math.Round(hk, 2);
                var matchCount = int.Parse(reader[10].ToString());
                matchCount++;
                var matchWins = int.Parse(reader[11].ToString());
                var matchDefeats = int.Parse(reader[12].ToString());
                var lastMatch = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                connectionReserved.Open();
                if (WinnerDefine(filePath) == 0)
                {
                    if (player.PlayerTeam == "T")
                    {
                        matchWins += 1;
                        var kdTemp = player.PlayerKill / (double)player.PlayerDeath;
                        kdTemp = Math.Round(kdTemp, 2);
                        thisPlayer.PlayerRank = Tools.DefinePlayerRankPts(thisPlayer.PlayerSteamId, thisPlayer.PlayerRank, true, "T");
                        var currentRankName = Tools.DefinePlayerRankName(thisPlayer.PlayerRank);
                        var insertQuery = new MySqlCommand("UPDATE kTVCSS.players SET Name = '" + thisPlayer.PlayerName + "', Kills = '" + thisPlayer.PlayerKill + "', Deaths = '" + thisPlayer.PlayerDeath + "', Headshots = '" + thisPlayer.PlayerHeadshot + "', KD = '" + kd + "', HK = '" + hk + "', RankPTS = '" + thisPlayer.PlayerRank + "', RankName = '" + currentRankName + "', MatchesPlayed = '" + matchCount + "', LastMatch = '" + lastMatch + "', MatchesVictories = '" + matchWins + "' WHERE SteamID = '" + thisPlayer.PlayerSteamId + "';", connectionReserved);
                        insertQuery.ExecuteNonQuery();
                        GetPlayerResult(thisPlayer.PlayerName.Replace("\t", ""), player.PlayerKill.ToString().Replace("\t", ""), player.PlayerDeath.ToString().Replace("\t", ""), kdTemp.ToString().Replace("\t", ""), tempHk.ToString().Replace("\t", ""));
                        SendStatsToUser(player.PlayerSteamId, "Победа", player.PlayerKill.ToString(), player.PlayerDeath.ToString(), player.PlayerHeadshot.ToString(), kdTemp.ToString(), Math.Round((tempHk * 100), 2).ToString() + "%");
                    }
                    else
                    {
                        matchDefeats += 1;
                        var kdTemp = player.PlayerKill / (double)player.PlayerDeath;
                        kdTemp = Math.Round(kdTemp, 2);
                        thisPlayer.PlayerRank = Tools.DefinePlayerRankPts(thisPlayer.PlayerSteamId, thisPlayer.PlayerRank, false, "CT");
                        var currentRankName = Tools.DefinePlayerRankName(thisPlayer.PlayerRank);
                        var insertQuery = new MySqlCommand("UPDATE kTVCSS.players SET Name = '" + thisPlayer.PlayerName + "', Kills = '" + thisPlayer.PlayerKill + "', Deaths = '" + thisPlayer.PlayerDeath + "', Headshots = '" + thisPlayer.PlayerHeadshot + "', KD = '" + kd + "', HK = '" + hk + "', RankPTS = '" + thisPlayer.PlayerRank + "', RankName = '" + currentRankName + "', MatchesPlayed = '" + matchCount + "', LastMatch = '" + lastMatch + "', MatchesDefeats = '" + matchDefeats + "' WHERE SteamID = '" + thisPlayer.PlayerSteamId + "';", connectionReserved);
                        insertQuery.ExecuteNonQuery();
                        GetPlayerResult(thisPlayer.PlayerName.Replace("\t", ""), player.PlayerKill.ToString().Replace("\t", ""), player.PlayerDeath.ToString().Replace("\t", ""), kdTemp.ToString().Replace("\t", ""), tempHk.ToString().Replace("\t", ""));
                        SendStatsToUser(player.PlayerSteamId, "Поражение", player.PlayerKill.ToString(), player.PlayerDeath.ToString(), player.PlayerHeadshot.ToString(), kdTemp.ToString(), Math.Round((tempHk * 100), 2).ToString() + "%");
                    }
                }
                if (WinnerDefine(filePath) == 1)
                {
                    if (player.PlayerTeam == "CT")
                    {
                        matchWins += 1;
                        var kdTemp = player.PlayerKill / (double)player.PlayerDeath;
                        kdTemp = Math.Round(kdTemp, 2);
                        thisPlayer.PlayerRank = Tools.DefinePlayerRankPts(thisPlayer.PlayerSteamId, thisPlayer.PlayerRank, true, "CT");
                        var currentRankName = Tools.DefinePlayerRankName(thisPlayer.PlayerRank);
                        var insertQuery = new MySqlCommand("UPDATE kTVCSS.players SET Name = '" + thisPlayer.PlayerName + "', Kills = '" + thisPlayer.PlayerKill + "', Deaths = '" + thisPlayer.PlayerDeath + "', Headshots = '" + thisPlayer.PlayerHeadshot + "', KD = '" + kd + "', HK = '" + hk + "', RankPTS = '" + thisPlayer.PlayerRank + "', RankName = '" + currentRankName + "', MatchesPlayed = '" + matchCount + "', LastMatch = '" + lastMatch + "', MatchesVictories = '" + matchWins + "' WHERE SteamID = '" + thisPlayer.PlayerSteamId + "';", connectionReserved);
                        insertQuery.ExecuteNonQuery();
                        GetPlayerResult(thisPlayer.PlayerName.Replace("\t", ""), player.PlayerKill.ToString().Replace("\t", ""), player.PlayerDeath.ToString().Replace("\t", ""), kdTemp.ToString().Replace("\t", ""), tempHk.ToString().Replace("\t", ""));
                        SendStatsToUser(player.PlayerSteamId, "Победа", player.PlayerKill.ToString(), player.PlayerDeath.ToString(), player.PlayerHeadshot.ToString(), kdTemp.ToString(), Math.Round((tempHk * 100), 2).ToString() + "%");
                    }
                    else
                    {
                        matchDefeats += 1;
                        var kdTemp = player.PlayerKill / (double)player.PlayerDeath;
                        kdTemp = Math.Round(kdTemp, 2);
                        thisPlayer.PlayerRank = Tools.DefinePlayerRankPts(thisPlayer.PlayerSteamId, thisPlayer.PlayerRank, false, "T");
                        var currentRankName = Tools.DefinePlayerRankName(thisPlayer.PlayerRank);
                        var insertQuery = new MySqlCommand("UPDATE kTVCSS.players SET Name = '" + thisPlayer.PlayerName + "', Kills = '" + thisPlayer.PlayerKill + "', Deaths = '" + thisPlayer.PlayerDeath + "', Headshots = '" + thisPlayer.PlayerHeadshot + "', KD = '" + kd + "', HK = '" + hk + "', RankPTS = '" + thisPlayer.PlayerRank + "', RankName = '" + currentRankName + "', MatchesPlayed = '" + matchCount + "', LastMatch = '" + lastMatch + "', MatchesDefeats = '" + matchDefeats + "' WHERE SteamID = '" + thisPlayer.PlayerSteamId + "';", connectionReserved);
                        insertQuery.ExecuteNonQuery();
                        GetPlayerResult(thisPlayer.PlayerName.Replace("\t", ""), player.PlayerKill.ToString().Replace("\t", ""), player.PlayerDeath.ToString().Replace("\t", ""), kdTemp.ToString().Replace("\t", ""), tempHk.ToString().Replace("\t", ""));
                        SendStatsToUser(player.PlayerSteamId, "Поражение", player.PlayerKill.ToString(), player.PlayerDeath.ToString(), player.PlayerHeadshot.ToString(), kdTemp.ToString(), Math.Round((tempHk * 100), 2).ToString() + "%");
                    }
                }
                connectionReserved.Close();
                connection.Close();
            }
        }

        private static void InsertPlayerResultsAfterMatch(string filePath)
        {
            var playerDetails = "";
            var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            for (int i = 0; i < _playerStatsAfterMatch.Count(); i++)
            {
                try
                {
                    var input = _playerStatsAfterMatch[i].Split('\t');
                    var query = new MySqlCommand("INSERT INTO `ktvcss`.`matchresults` (`MatchId`, `TeamName`, `NickName`, `Kills`, `Deaths`, `KD`, `HK`) VALUES ('" + input[0].ToString() + "', '" + input[1] + "', '" + input[2] + "', '" + input[3] + "', '" + input[4] + "', '" + input[5] + "', '" + input[6] + "');", connection);
                    query.ExecuteNonQuery();
                    playerDetails += input[2] + " [" + input[3] + "-" + input[4] + "] @ KDR: " + input[5] + " HPK: " + input[6] + "\r\n";
                }
                catch (Exception)
                {
                    //
                }
            }
            connection.Close();
            var web = new WebClient();
            try
            {
                var mapName = Tools.GetMapName(filePath);
                var matchRez = SendMatchResults(filePath, false, false);
                var matchInfo = matchRez[0] + " [" + matchRez[1] + "-" + matchRez[3] + "] " + matchRez[2];
                //if (matchInfo.Contains("MixTeam")) return;
                var message = "Матч от " + DateTime.Now.ToString("dd-MM-yyyy HH:mm") + "\r\n\r\n" + matchInfo + "\r\n\r\nКарта: " + mapName + "\r\n\r\n" + playerDetails;
                var api = new VkApi();
                api.Authorize(new ApiAuthParams
                {
                    AccessToken = Token,
                });
                var wallPostParams = new WallPostParams
                {
                    OwnerId = -WorkClass.StatGroupId,
                    Message = message,
                    FromGroup = true,
                    Signed = false
                };
                var imagePath = Tools.GetMapImage(mapName);
                if (imagePath != "")
                {
                    var image = System.Drawing.Image.FromFile(imagePath);
                    var graphics = Graphics.FromImage(image);
                    var rectangleA = new Rectangle(640, 190, 0, 200);
                    var rectangleB = new Rectangle(640, 470, 0, 200);
                    var rectangleC = new Rectangle(640, 300, 0, 200);
                    var rectangleAx = new Rectangle(638, 194, 0, 200);
                    var rectangleBx = new Rectangle(638, 474, 0, 200);
                    var rectangleCx = new Rectangle(638, 304, 0, 200);
                    var sizeA = matchRez[0].Length < 10 ? 72f : 56;
                    var sizeB = matchRez[2].Length < 10 ? 72f : 56;
                    Tools.DrawText(graphics, matchRez[0].ToUpper(), rectangleA, StringAlignment.Center, sizeA, Brushes.Black);
                    Tools.DrawText(graphics, matchRez[2].ToUpper(), rectangleB, StringAlignment.Center, sizeB, Brushes.Black);
                    Tools.DrawText(graphics, matchRez[1] + "-" + matchRez[3], rectangleC, StringAlignment.Center, 108, Brushes.Black);
                    Tools.DrawText(graphics, matchRez[0].ToUpper(), rectangleAx, StringAlignment.Center, sizeA, Brushes.White);
                    Tools.DrawText(graphics, matchRez[2].ToUpper(), rectangleBx, StringAlignment.Center, sizeB, Brushes.White);
                    Tools.DrawText(graphics, matchRez[1] + "-" + matchRez[3], rectangleCx, StringAlignment.Center, 108, Brushes.White);

                    image.Save(imagePath.Replace(".jpg", "_temp.jpg"), System.Drawing.Imaging.ImageFormat.Jpeg);

                    var uploadServer = api.Photo.GetWallUploadServer(WorkClass.StatGroupId);
                    var result = Encoding.ASCII.GetString(web.UploadFile(uploadServer.UploadUrl, imagePath.Replace(".jpg", "_temp.jpg")));
                    var photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.StatGroupId);
                    wallPostParams.Attachments = new List<MediaAttachment>
                    {
                        photo.FirstOrDefault()
                    };
                }
                var postId = api.Wall.Post(wallPostParams);
                //disabled story upload
                /*try
                {
                    if (imagePath != "")
                    {
                        // upload story to vk
                        #warning disabled story upload
                        var simage = System.Drawing.Image.FromFile(imagePath);
                        var sgraphics = Graphics.FromImage(simage);
                        var srectangleA = new Rectangle(640, 200, 0, 200);
                        var srectangleB = new Rectangle(640, 450, 0, 200);
                        var srectangleC = new Rectangle(640, 300, 0, 200);
                        var srectangleAx = new Rectangle(638, 204, 0, 200);
                        var srectangleBx = new Rectangle(638, 454, 0, 200);
                        var srectangleCx = new Rectangle(638, 304, 0, 200);
                        var ssizeA = matchRez[0].Length < 10 ? 36f : 28;
                        var ssizeB = matchRez[2].Length < 10 ? 36f : 28;
                        DrawText(sgraphics, matchRez[0].ToUpper(), srectangleA, StringAlignment.Center, ssizeA, Brushes.Black);
                        DrawText(sgraphics, matchRez[2].ToUpper(), srectangleB, StringAlignment.Center, ssizeB, Brushes.Black);
                        DrawText(sgraphics, matchRez[1] + "-" + matchRez[3], srectangleC, StringAlignment.Center, 54, Brushes.Black);
                        DrawText(sgraphics, matchRez[0].ToUpper(), srectangleAx, StringAlignment.Center, ssizeA, Brushes.White);
                        DrawText(sgraphics, matchRez[2].ToUpper(), srectangleBx, StringAlignment.Center, ssizeB, Brushes.White);
                        DrawText(sgraphics, matchRez[1] + "-" + matchRez[3], srectangleCx, StringAlignment.Center, 54, Brushes.White);

                        simage.Save(imagePath.Replace(".jpg", "_story.jpg"), System.Drawing.Imaging.ImageFormat.Jpeg);

                        VkNet.Model.RequestParams.Stories.GetPhotoUploadServerParams getPhotoUploadServerParams = new VkNet.Model.RequestParams.Stories.GetPhotoUploadServerParams()
                        {
                            AddToNews = true,
                            GroupId = MainGroupId,
                            LinkUrl = "https://vk.com/ktvcss?w=wall-WorkClass.StatGroupId_" + postId,
                            LinkText = StoryLinkText.LearnMore
                        };
                        var storyServer = api.Stories.GetPhotoUploadServer(getPhotoUploadServerParams);
                        var storyResult = Encoding.ASCII.GetString(web.UploadFile(storyServer.UploadUrl, imagePath.Replace(".jpg", "_story.jpg")));
                        var reg = Regex.Matches(storyResult, @"""([^""\\]*|\\[""\\bfnrt\/]|\\u[0-9a-f]{4})*""", RegexOptions.Multiline);
                        var parameters = new VkParameters()
                        {
                            { "upload_results", reg[2].Value.Replace("\"", "") }
                        };
                        api.Call("stories.save", parameters);
                        // uploaded
                    }
                }
                catch (Exception)
                {
                    // Ignored
                }
                File.Delete(imagePath.Replace(".jpg", "_story.jpg"));
                */
                File.Delete(imagePath.Replace(".jpg", "_temp.jpg"));
            }
            catch (Exception ex)
            {
                PrintLogMessage(ex.ToString(), "ERROR");
            }
        }

        private static void PlayerStatisticsProcessing(object dirPath)
        {
            while (true)
            {
                LogGrabber();
                var directory = new DirectoryInfo(dirPath.ToString());
                var files = directory.GetFiles("*.log", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        Console.Title = ProgramName + " @ " + file.DirectoryName + " - " + file.Name;
                        PrintLogMessage("started processing file " + file.Name, "INFO");
                        //HighLights(file.FullName);
                        MultiCountProcessing(file.FullName);
                        WinnerDefine(file.FullName);
                        UpdatePlayerStats(file.FullName);
                        Tools.DefinePlayerRankAfterCalibration();
                        DefineTeamRankAfterCalibration();
                        SendMatchResults(file.FullName, true, true);
                        if (!Directory.Exists("Backup"))
                        {
                            Directory.CreateDirectory("Backup");
                        }
                        if (!Directory.Exists("Tournaments"))
                        {
                            Directory.CreateDirectory("Tournaments");
                        }
                        if (!Directory.Exists(@"Backup\" + file.Directory.Name))
                        {
                            Directory.CreateDirectory(@"Backup\" + file.Directory.Name);
                        }
                        var filePath = @"Tournaments\" + DateTime.Now.ToString("yyyy-MM-dd hh_mm_ss") + ".csv";
                        File.WriteAllLines(filePath, _playerStatsAfterMatch.ToArray());
                        InsertPlayerResultsAfterMatch(file.FullName);
                        _playerStatsAfterMatch.Clear();
                        _playerList.Clear();
                        _playerInUse.Clear();
                        File.Copy(file.FullName, @"Backup\" + file.Directory.Name + @"\" + Path.GetFileNameWithoutExtension(file.FullName) + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log", true);
                        File.Delete(file.FullName);
                        PrintLogMessage("successfully processed file " + file.Name, "INFO");
                        Console.Title = ProgramName;
                    }
                    catch (Exception ex)
                    {
                        PrintLogMessage(ex.ToString() + " " + file.FullName, "ERROR");
                        if (!Directory.Exists("Failed"))
                        {
                            Directory.CreateDirectory("Failed");
                            if (!Directory.Exists(@"Failed\" + file.Directory.Name))
                            {
                                Directory.CreateDirectory(@"Failed\" + file.Directory.Name);
                            }
                        }
                        try
                        {
                            File.Copy(file.FullName, @"Failed\" + Path.GetFileNameWithoutExtension(file.FullName) + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log", true);
                            File.Delete(file.FullName);
                        }
                        catch (Exception)
                        {
                            File.Delete(file.FullName);
                        }
                        Environment.Exit(0);
                    }
                    Thread.Sleep(1000);
                }
                Thread.Sleep(20000);
            }
        }

        private static string HighLights(string fullName)
        {
            var lines = File.ReadAllLines(fullName, Encoding.UTF8);
            foreach (var line in lines)
            {
                if (!line.Contains("killed")) continue;
                var playersIds = new List<string>();
                var playersNames = new List<string>();
                var steamIds = new List<string>();
                var playersTeams = new List<string>();
                var splitted = Regex.Split(line, @".killed.""");
                foreach (var split in splitted)
                {
                    var playerIdRe = Regex.Matches(split, @"\<\d+\>");
                    var idCount = playerIdRe.Count - 1;
                    var playerId = playerIdRe[idCount].ToString();
                    var nameSplitter = playerId;
                    playerId = Regex.Replace(playerId, @"[\<\>]", "");
                    playersIds.Add(playerId);

                    var playerName = Regex.Split(split, nameSplitter)[0];
                    playerName = Regex.Replace(playerName, @".+?""", "");
                    playersNames.Add(playerName);

                    var steamIdRe = Regex.Matches(split, @"\<STEAM_.+?\>");
                    idCount = steamIdRe.Count - 1;
                    var steamId = steamIdRe[idCount].ToString();
                    steamId = Regex.Replace(steamId, @"[\<\>]", "");
                    steamIds.Add(steamId);

                    var playersTeamsRe = Regex.Matches(split, @"\<[A-Z]+\>");
                    idCount = playersTeamsRe.Count - 1;
                    var playerTeamIs = playersTeamsRe[idCount].ToString();
                    playerTeamIs = Regex.Replace(playerTeamIs, @"[\<\>]", "");
                    playerTeamIs = playerTeamIs.Contains("TERROR") ? "T" : "CT";
                    playersTeams.Add(playerTeamIs);
                }
            }
            return "";
        }

        public static void PrintLogMessage(string message, string logLevel)
        {
            try
            {
                _log.Add(DateTime.Now + " [" + logLevel + "] " + message);
                File.WriteAllLines("log.txt", _log);
            }
            catch (Exception)
            {
                // Ignored
            }
            Console.WriteLine(DateTime.Now + " [" + logLevel + "] " + message);
        }

        private static void TeamProcessing(string teamName, string filePath, int winner)
        {
            try
            {
                var lastMatch = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var connection = new MySqlConnection(ConnectionString);
                connection.Open();
                var query = new MySqlCommand("SELECT RankPTS, MatchesPlayed, MatchesVictories, MatchesDefeats FROM kTVCSS.teams WHERE Name = '" + teamName + "'", connection);
                var reader = query.ExecuteReader();
                if (!reader.HasRows)
                {
                    reader.Close();
                    if (winner == 0)
                        query = new MySqlCommand("INSERT INTO `ktvcss`.`teams` (`Name`, `RankPTS`, `MatchesPlayed`, `MatchesVictories`, `MatchesDefeats`, `IsCalibration`, `LastMatch`, `Staff`, `CapID`) VALUES ('" + teamName + "', '1025', '1', '1', '0', '0', '" + lastMatch + "', 'NULL', '0');", connection);
                    else query = new MySqlCommand("INSERT INTO `ktvcss`.`teams` (`Name`, `RankPTS`, `MatchesPlayed`, `MatchesVictories`, `MatchesDefeats`, `IsCalibration`, `LastMatch`, `Staff`, `CapID`) VALUES ('" + teamName + "', '975', '1', '0', '1', '0', '" + lastMatch + "', 'NULL', '0');", connection);
                    query.ExecuteNonQuery();
                }
                else
                {
                    reader.Read();
                    if (winner == 0)
                    {
                        var pts = int.Parse(reader[0].ToString());
                        var mp = int.Parse(reader[1].ToString());
                        var mv = int.Parse(reader[2].ToString());
                        reader.Close();
                        pts += 25;
                        mp += 1;
                        mv += 1;
                        query.CommandText = "UPDATE kTVCSS.teams SET RankPTS = '" + pts + "', MatchesPlayed = '" + mp + "', MatchesVictories = '" + mv + "', LastMatch = '" + lastMatch + "' WHERE Name = '" + teamName + "';";
                        query.ExecuteNonQuery();
                    }
                    else
                    {
                        var pts = int.Parse(reader[0].ToString());
                        var mp = int.Parse(reader[1].ToString());
                        var md = int.Parse(reader[3].ToString());
                        reader.Close();
                        pts -= 25;
                        mp += 1;
                        md += 1;
                        query.CommandText = "UPDATE kTVCSS.teams SET RankPTS = '" + pts + "', MatchesPlayed = '" + mp + "', MatchesDefeats = '" + md + "', LastMatch = '" + lastMatch + "' WHERE Name = '" + teamName + "';";
                        query.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (Exception exp)
            {
                PrintLogMessage(exp.ToString(), "ERROR");
            }
        }

        private static void DefineTeamRankAfterCalibration()
        {
            var id = new List<int>();
            var rankPts = new List<int>();
            var matchesVictories = new List<int>();
            var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT ID, RankPTS, MatchesVictories FROM kTVCSS.teams WHERE IsCalibration = '1' AND MatchesPlayed = '10';", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                id.Add(int.Parse(reader[0].ToString()));
                rankPts.Add(int.Parse(reader[1].ToString()));
                matchesVictories.Add(int.Parse(reader[2].ToString()));
            }
            reader.Close();
            if (!id.Any()) return;
            for (var i = 0; i < id.Count(); i++)
            {
                var pts = 0;
                if (matchesVictories[i] == 10)
                {
                    pts = 1500 + rankPts[i];
                }
                if (matchesVictories[i] == 9)
                {
                    pts = 1400 + rankPts[i];
                }
                if (matchesVictories[i] == 8)
                {
                    pts = 1300 + rankPts[i];
                }
                if (matchesVictories[i] == 7)
                {
                    pts = 1200 + rankPts[i];
                }
                if (matchesVictories[i] == 6)
                {
                    pts = 1100 + rankPts[i];
                }
                if (matchesVictories[i] == 5)
                {
                    pts = 1000 + rankPts[i];
                }
                if (matchesVictories[i] == 4)
                {
                    pts = 900 + rankPts[i];
                }
                if (matchesVictories[i] == 3)
                {
                    pts = 800 + rankPts[i];
                }
                if (matchesVictories[i] == 2)
                {
                    pts = 700 + rankPts[i];
                }
                if (matchesVictories[i] == 1)
                {
                    pts = 600 + rankPts[i];
                }
                if (matchesVictories[i] == 0)
                {
                    pts = 500 + rankPts[i];
                }
                var queryUpdate = new MySqlCommand("UPDATE kTVCSS.teams SET RankPTS = '" + pts + "', IsCalibration = '0' WHERE ID = '" + id[i] + "';", connection);
                queryUpdate.ExecuteNonQuery();
            }
            connection.Close();
        }

        private static void LoadConfig()
        {
            ConnectionString = Properties.Settings.Default.mysqlConnectionString;
            var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT * FROM servers WHERE Enabled = 1", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                var server = new Servers
                {
                    Host = reader[2].ToString(),
                    UserName = reader[3].ToString(),
                    UserPassword = reader[4].ToString(),
                    Port = int.Parse(reader[5].ToString()),
                    LogsDir = reader[6].ToString(),
                    GamePort = int.Parse(reader[7].ToString()),
                    RconPassword = reader[8].ToString()
                };
                ServerList.Add(server);
                PrintLogMessage($"Loaded server {server.UserName}@{server.Host}:{server.GamePort}", "DEBUG");
            }
            reader.Close();
            query = new MySqlCommand("SELECT * FROM statsettings", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                MainGroupId = Convert.ToInt32(reader[0].ToString());
                StatGroupId = Convert.ToInt32(reader[1].ToString());
                AdminUserId = Convert.ToInt32(reader[2].ToString());
            }
            connection.Close();
        }

        private static void LogGrabber()
        {
            foreach (var server in ServerList)
            {
                Console.Title = ProgramName + " @ Scanning " + server.UserName + "@" + server.Host;
                try
                {
                    var request = (FtpWebRequest)WebRequest.Create("ftp://" + server.UserName + ":" + server.UserPassword + "@" + server.Host + ":" + server.Port + server.LogsDir);
                    request.Method = WebRequestMethods.Ftp.ListDirectory;
                    var response = (FtpWebResponse)request.GetResponse();
                    var responseStream = response.GetResponseStream();
                    var reader = new StreamReader(responseStream);
                    var files = reader.ReadToEnd();
                    var fileList = files.Split('\n');
                    for (int i = 0; i < fileList.Count() - 1; i++)
                    {
                        fileList[i] = fileList[i].Replace("\r", "");
                        if (fileList[i].Length < 5) continue;
                        using (var web = new WebClient())
                        {
                            try
                            {
                                var log = web.DownloadString("ftp://" + server.UserName + ":" + server.UserPassword + "@" + server.Host + ":" + server.Port + server.LogsDir + fileList[i]);
                                if (log.Contains("WarMod triggered \"full_time\"") || log.Contains("WarMod triggered \"over_full_time\""))
                                {
                                    PrintLogMessage("downloaded file " + fileList[i], "INFO");
                                    if (!Directory.Exists(@"Logs\" + server.UserName + "_" + server.Host))
                                    {
                                        Directory.CreateDirectory(@"Logs\" + server.UserName + "_" + server.Host);
                                    }
                                    File.WriteAllText(@"Logs\" + server.UserName + "_" + server.Host + @"\" + fileList[i], log, Encoding.GetEncoding(1251));
                                    var reqFtp = (FtpWebRequest)WebRequest.Create(new Uri("ftp://" + server.Host + ":" + server.Port + server.LogsDir + fileList[i]));
                                    reqFtp.Credentials = new NetworkCredential(server.UserName, server.UserPassword);
                                    reqFtp.KeepAlive = false;
                                    reqFtp.Method = WebRequestMethods.Ftp.DeleteFile;
                                    var result = string.Empty;
                                    var _response = (FtpWebResponse)reqFtp.GetResponse();
                                    var size = _response.ContentLength;
                                    var datastream = _response.GetResponseStream();
                                    var sr = new StreamReader(datastream);
                                    result = sr.ReadToEnd();
                                    sr.Close();
                                    datastream.Close();
                                    _response.Close();
                                }
                            }
                            catch (Exception exp)
                            {
                                if (!exp.Message.Contains("550"))
                                    PrintLogMessage(exp.ToString() + " " + fileList[i], "ERROR");
                                try
                                {
                                    File.Copy(fileList[i], @"Failed\" + fileList[i] + "_" + DateTime.Now, true);
                                    File.Delete(fileList[i]);
                                }
                                catch (Exception)
                                {
                                    // Ignored
                                }
                            }
                        }
                    }
                    reader.Close();
                    responseStream.Close();
                    response.Close();
                }
                catch (Exception exp)
                {
                    PrintLogMessage(exp.ToString(), "ERROR");
                }
            }
            Console.Title = ProgramName;
        }

        private static string[] SendMatchResults(string filePath, bool anounce, bool teamProcessing)
        {
            var serverId = 0;
            try
            {
                var server = new FileInfo(filePath).Directory.Name.Split('_');
                var mysqlConnection = new MySqlConnection(ConnectionString);
                mysqlConnection.Open();
                var query = new MySqlCommand($"SELECT Id FROM servers WHERE Host = '{server[1]}' AND UserName = '{server[0]}'", mysqlConnection);
                using (var reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        serverId = Convert.ToInt32(reader[0].ToString());
                    }
                }
                mysqlConnection.Close();
            }
            catch (Exception)
            {
                // ignored
            }
            var T = new List<string>();
            var ct = new List<string>();
            var nicknamesT = new List<string>();
            var nicknamesCt = new List<string>();
            var matchResult = GetMatchResults(filePath);
            foreach (var player in _playerList)
            {
                if (player.PlayerTeam == "T")
                {
                    T.Add(player.PlayerName);
                }
                else ct.Add(player.PlayerName);
            }
            foreach (var buddy in T)
            {
                try
                {
                    var temp = buddy.Split(' ');
                    nicknamesT.Add(temp[0]);
                }
                catch (Exception)
                {
                    nicknamesT.Add(buddy);
                }
            }
            foreach (var buddy in ct)
            {
                try
                {
                    var temp = buddy.Split(' ');
                    nicknamesCt.Add(temp[0]);
                }
                catch (Exception)
                {
                    nicknamesCt.Add(buddy);
                }
            }
            var id = Tools.GetLastMatchId();
            foreach (var possibleTag in nicknamesT)
            {
                if (nicknamesT.Count(x => x == possibleTag) >= 3)
                {
                    matchResult[0] = possibleTag;
                    break;
                }
            }
            foreach (var possibleTag in nicknamesCt)
            {
                if (nicknamesCt.Count(x => x == possibleTag) >= 3)
                {
                    matchResult[2] = possibleTag;
                    break;
                }
            }
            if (matchResult[0] == "Terrorists")
            {
                matchResult[0] = "MixTeam " + T[0];
            }
            if (matchResult[2] == "Counter-Terrorists")
            {
                matchResult[2] = "MixTeam " + ct[0];
            }
            //
            for (int i = 0; i < _playerStatsAfterMatch.Count(); i++)
            {
                var player = _playerStatsAfterMatch[i].Split('\t');
                foreach (var pl in ct)
                {
                    if (player[0] == pl)
                    {
                        _playerStatsAfterMatch[i] = id + "\t" + matchResult[2] + "\t" + _playerStatsAfterMatch[i];
                    }
                }
                foreach (var pl in T)
                {
                    if (player[0] == pl)
                    {
                        _playerStatsAfterMatch[i] = id + "\t" + matchResult[0] + "\t" + _playerStatsAfterMatch[i];
                    }
                }
            }
            var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            var map = Tools.GetMapName(filePath);
            if (anounce)
            {
                var query = new MySqlCommand("INSERT INTO kTVCSS.matches (`TeamA`, `TeamAScore`, `TeamB`, `TeamBScore`, `MatchDate`, `Map`, `ServerId`) VALUES ('" + matchResult[0] + "', '" + matchResult[1] + "', '" + matchResult[2] + "', '" + matchResult[3] + "', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + map + "', '" + serverId + "');", connection);
                query.ExecuteNonQuery();
            }
            connection.Close();
            if (teamProcessing)
            {
                if (!matchResult[0].Contains("MixTeam"))
                {
                    if (int.Parse(matchResult[1]) > int.Parse(matchResult[3]))
                        TeamProcessing(matchResult[0], filePath, 0);
                    else TeamProcessing(matchResult[0], filePath, 1);
                }
                if (!matchResult[2].Contains("MixTeam"))
                {
                    if (int.Parse(matchResult[1]) > int.Parse(matchResult[3]))
                        TeamProcessing(matchResult[2], filePath, 1);
                    else TeamProcessing(matchResult[2], filePath, 0);
                }
            }

            if (anounce)
                AnonceMatchResults(matchResult[0], matchResult[1], matchResult[2], matchResult[3], map);
            return matchResult;
        }

        // need fix
        private static void AutoTeamNameSetup(object param)
        {
            var serverConfig = param.ToString().Split(';');
            while (true)
            {
                try
                {
                    var matchResult = new string[2];
                    var t = new List<string>();
                    var ct = new List<string>();
                    var tTags = new List<string>();
                    var ctTags = new List<string>();
                    var webClient = new WebClient()
                    {
                        Encoding = Encoding.UTF8
                    };
                    var webRequest = webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + serverConfig[0].ToString() + "&port=" + serverConfig[1].ToString() + "&password=" + serverConfig[2].ToString() + "&command=sm_usrlst");
                    var result = webRequest.Split('\n');
                    result[0] = result[0].Substring(result[0].IndexOf('"') + 1);
                    foreach (var item in result)
                    {
                        try
                        {
                            var player = item.Split(';');
                            if (player[1] == "2") t.Add(player[0]);
                            if (player[1] == "3") ct.Add(player[0]);
                        }
                        catch (Exception)
                        {
                            break;
                        }
                    }
                    foreach (var buddy in t)
                    {
                        try
                        {
                            tTags.Add(buddy.Split(' ')[0]);
                        }
                        catch (Exception)
                        {
                            tTags.Add(buddy);
                        }
                    }
                    foreach (var buddy in ct)
                    {
                        try
                        {
                            ctTags.Add(buddy.Split(' ')[0]);
                        }
                        catch (Exception)
                        {
                            ctTags.Add(buddy);
                        }
                    }
                    var ctExist = false;
                    var tExist = false;
                    foreach (var possibleTag in tTags)
                    {
                        if (tTags.Count(x => x == possibleTag) >= 3)
                        {
                            tExist = true;
                            matchResult[0] = possibleTag;
                            break;
                        }
                    }
                    foreach (var possibleTag in ctTags)
                    {
                        if (ctTags.Count(x => x == possibleTag) >= 3)
                        {
                            ctExist = true;
                            matchResult[1] = possibleTag;
                            break;
                        }
                    }
                    if (matchResult[0] == null)
                    {
                        try
                        {
                            tExist = true;
                            matchResult[0] = "MixTeam " + tTags[0];
                        }
                        catch (Exception)
                        {
                            //matchResult[0] = "<TERRORISTS>";
                        }
                    }
                    if (matchResult[1] == null)
                    {
                        try
                        {
                            ctExist = true;
                            matchResult[1] = "MixTeam " + ctTags[0];
                        }
                        catch (Exception)
                        {
                            //matchResult[1] = "<CT>";
                        }
                    }
                    if (ctExist && tExist)
                    {
                        webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + serverConfig[0].ToString() + "&port=" + serverConfig[1].ToString() + "&password=" + serverConfig[2].ToString() + "&command=clientmod_team_t " + "<TERRORISTS> " + matchResult[0]);
                        Thread.Sleep(1000);
                        webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + serverConfig[0].ToString() + "&port=" + serverConfig[1].ToString() + "&password=" + serverConfig[2].ToString() + "&command=clientmod_team_ct " + "<CT> " + matchResult[1]);
                        Thread.Sleep(1000);
                        webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + serverConfig[0].ToString() + "&port=" + serverConfig[1].ToString() + "&password=" + serverConfig[2].ToString() + "&command=tv_title " + matchResult[0] + " vs " + matchResult[1]);
                    }
                    // Match is not in progress
                    var IsBusyServer = webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + serverConfig[0].ToString() + "&port=" + serverConfig[1].ToString() + "&password=" + serverConfig[2].ToString() + "&command=score");
                    if (IsBusyServer.Contains("Match is not in progress"))
                    {
                        var connection = new MySqlConnection(ConnectionString);
                        connection.Open();
                        var query = new MySqlCommand($"UPDATE ktvcss.servers SET Busy = 0 WHERE Host = '{serverConfig[0].ToString()}' AND GamePort = '{serverConfig[1].ToString()}'", connection);
                        query.ExecuteNonQuery();
                        connection.Close();
                    }
                    else
                    {
                        var connection = new MySqlConnection(ConnectionString);
                        connection.Open();
                        var query = new MySqlCommand($"UPDATE ktvcss.servers SET Busy = 1 WHERE Host = '{serverConfig[0].ToString()}' AND GamePort = '{serverConfig[1].ToString()}'", connection);
                        query.ExecuteNonQuery();
                        connection.Close();
                    }
                }
                catch (Exception exp)
                {
                    PrintLogMessage(exp.ToString(), "ERROR");
                }
                Thread.Sleep(15000);
            }
        }

        private static void TeamSetup()
        {
            foreach (var server in ServerList)
            {
                var thread = new Thread(AutoTeamNameSetup);
                thread.Start(server.Host + ";" + server.GamePort + ";" + server.RconPassword);
                Thread.Sleep(500);
            }
        }

        private static void AnonceMatchResults(string teamA, string teamAScore, string teamB, string teamBScore, string mapName)
        {
            var webClient = new WebClient();
            foreach (var server in ServerList)
            {
                try
                {
                    webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + server.Host + "&port=" + server.GamePort + "&password=" + server.RconPassword + "&command=say [KPR] Сыгран матч: " + teamA + " [" + teamAScore + "-" + teamBScore + "] " + teamB + " @ " + mapName);
                }
                catch (Exception ex)
                {
                    PrintLogMessage(ex.ToString(), "ERROR");
                }
            }
        }
        // by inzame
        private static void SendStatsToUser(string steamID, string matchResult, string kills, string deaths, string hs, string kd, string hk)
        {
            var connection = new MySqlConnection(ConnectionString);
            var _vkid = "";
            string messageFooter;
            var _sendEnabled = -1;
            connection.Open();
            try
            {
                var vkIdQuery = new MySqlCommand($"SELECT `VkID` FROM `players` WHERE `SteamID` = \"{steamID}\"", connection);
                var vkIdreader = vkIdQuery.ExecuteReader();
                while (vkIdreader.Read())
                {
                    _vkid = vkIdreader[0].ToString();
                }
                vkIdreader.Close();
            }
            catch (Exception ex)
            {
                PrintLogMessage(ex.ToString(), "ERROR");
                connection.Close();
                return;
            }

            try
            {
                var query = new MySqlCommand($"SELECT `SendStatistics` FROM `players` WHERE `VkID` = \"{_vkid}\" AND `SteamID` = \"{steamID}\"", connection);
                var sendEnabledReader = query.ExecuteReader();
                while (sendEnabledReader.Read())
                {
                    _sendEnabled = int.Parse(sendEnabledReader[0].ToString());
                }
                sendEnabledReader.Close();
                if (_sendEnabled < 1) return;
            }
            catch (Exception ex)
            {
                PrintLogMessage(ex.ToString(), "ERROR");
                connection.Close();
                return;
            }

            try
            {
                var api = new VkApi();
                api.Authorize(new ApiAuthParams
                {
                    AccessToken = Token,
                });
                var query = new MySqlCommand($"SELECT * FROM `players` WHERE `VkID` = \"{_vkid}\" AND `SteamID` = \"{steamID}\"", connection);
                var infoReader = query.ExecuteReader();
                string _nickname, _kills, _deaths, _hs, _kd, _pts, _rank, _wins, _loses, _date; // Конченный хак ...
                _nickname = _kills = _deaths = _hs = _kd = _pts = _rank = _wins = _loses = _date = ""; // дэээ
                int _position = 0;
                string word = "";
                while (infoReader.Read())
                {
                    _nickname = infoReader[1].ToString();
                    _kills = infoReader[3].ToString();
                    _deaths = infoReader[4].ToString();
                    _hs = infoReader[5].ToString();
                    _kd = infoReader[6].ToString();
                    _pts = infoReader[8].ToString();
                    _rank = infoReader[9].ToString();
                    _wins = infoReader[11].ToString();
                    _loses = infoReader[12].ToString();
                    _date = infoReader[14].ToString();
                }
                infoReader.Close();

                var posQuery = new MySqlCommand("SELECT * FROM `players` ORDER BY `RankPTS` DESC", connection);
                var posReader = posQuery.ExecuteReader();
                int i = 0;
                while (posReader.Read())
                {
                    if (posReader[2].ToString().Contains(steamID))
                    {
                        _position = i + 1;
                        break;
                    }
                    i++;
                }
                posReader.Close();

                var rankImagePath = "";
                var firstName = api.Users.Get(new long[] { int.Parse(_vkid) }).FirstOrDefault().FirstName;
                var lastName = api.Users.Get(new long[] { int.Parse(_vkid) }).FirstOrDefault().LastName;
                var totalGames = int.Parse(_wins) + int.Parse(_loses);
                var _winrate = double.Parse(_wins) / double.Parse(totalGames.ToString());
                _winrate = Math.Round(_winrate, 2) * 100;
                var matchHeader = $"Статистика за матч от {_date}.\r\n";
                var matchStats = $"{matchResult}. Убийств: {kills}. Смертей: {deaths}. Хэдшотов: {hs}. K/D: {kd}. H/K: {hk}.\r\n";
                var statsHeader = $"{firstName} \"{_nickname}\" ваша текущая статистика.\r\n";
                var killsStats = $"Убийств: {_kills}. Смертей: {_deaths}. Хэдшотов: {_hs}. K/D: {_kd}.\r\n";
                var playStats = $"Побед: {_wins}. Поражений {_loses}. Процент побед: {_winrate}%.\r\n";
                if (totalGames >= 10)
                {
                    rankImagePath = Tools.GetRankImage(_rank);
                    messageFooter = $"Текущий рейтинг: {_pts}. Позиция в рейтинге: {_position}\r\nВаш текущий ранк: {_rank}\r\nПодробнее в группе: vk.com/ktvcss_kpr";
                }
                else
                {
                    rankImagePath = @"Images\Stats\unranked.png";
                    if ((10 - totalGames) == 1) word = "матч";
                    if ((10 - totalGames) >= 2 && (10 - totalGames) <= 4) word = "матча";
                    if ((10 - totalGames) >= 5 && (10 - totalGames) <= 9) word = "матчей";
                    messageFooter = $"Вы проходите калибровку. До конца калибровки {10 - totalGames} {word}.\r\nПодробнее в группе: vk.com/ktvcss_kpr";
                }

                var web = new WebClient();
                var image = System.Drawing.Image.FromFile(Tools.ImagePath(matchResult));
                var graphics = Graphics.FromImage(image);
                var myEncoderParameters = new EncoderParameters(1);
                var myEncoder = System.Drawing.Imaging.Encoder.Quality;
                var myEncoderParameter = new EncoderParameter(myEncoder, 100L);
                myEncoderParameters.Param[0] = myEncoderParameter;
                var myImageCodecInfo = Tools.GetEncoderInfo("image/jpeg");
                var rectangleA = new Rectangle(640, 190, 0, 200);
                var rectangleAx = new Rectangle(638, 194, 0, 200);
                graphics.DrawString(kd, new Font("Trebuchet MS", 40f), Brushes.Black, 1045, 334);
                graphics.DrawString(kd, new Font("Trebuchet MS", 40f), Brushes.White, 1047, 336);
                graphics.DrawString(hk, new Font("Trebuchet MS", 40f), Brushes.Black, 1045, 442);
                graphics.DrawString(hk, new Font("Trebuchet MS", 40f), Brushes.White, 1047, 444);
                var killsNdeaths = kills + "/" + deaths;
                graphics.DrawString(killsNdeaths, new Font("Trebuchet MS", 40f), Brushes.Black, 1045, 550);
                graphics.DrawString(killsNdeaths, new Font("Trebuchet MS", 40f), Brushes.White, 1047, 552);
                graphics.DrawImage(System.Drawing.Image.FromFile(rankImagePath), 620, 40);
                graphics.DrawString(_nickname, new Font("Trebuchet MS", 28f), Brushes.White, 800, 60);
                graphics.DrawString(firstName + " " + lastName, new Font("Trebuchet MS", 28f), Brushes.White, 800, 120);
                image.Save(Tools.ImagePath(matchResult).Replace(".jpg", "_temp.jpg"), myImageCodecInfo, myEncoderParameters);

                var uploadServer = api.Photo.GetWallUploadServer(WorkClass.StatGroupId);
                var result = Encoding.ASCII.GetString(web.UploadFile(uploadServer.UploadUrl, Tools.ImagePath(matchResult).Replace(".jpg", "_temp.jpg")));
                var photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.StatGroupId);

                var messageParams = new MessagesSendParams
                {
                    GroupId = (ulong)MainGroupId,
                    UserId = int.Parse(_vkid),
                    RandomId = new Random().Next(),
                    Message = matchHeader + matchStats + "\r\n" + statsHeader + killsStats + playStats + messageFooter,
                    Attachments = new List<MediaAttachment>
                    {
                        photo.FirstOrDefault()
                    }
                };
                Thread.Sleep(1000);
                api.Messages.Send(messageParams);
                File.Delete(Tools.ImagePath(matchResult).Replace(".jpg", "_temp.jpg"));
            }
            catch (Exception)
            {
                //PrintLogMessage(ex.ToString(), "ERROR");
                try
                {
                    File.Delete(Tools.ImagePath(matchResult).Replace(".jpg", "_temp.jpg"));
                }
                catch (Exception)
                {
                    // Ignored path not found
                }
                connection.Close();
                return;
            }
            connection.Close();
        }

        static void Main(string[] args)
        {
            Console.Title = "KPR v2.0.0";
            Console.ForegroundColor = ConsoleColor.Green;

            if (!Directory.Exists("Logs")) Directory.CreateDirectory("Logs");

            if (!File.Exists("log.txt")) File.Create("log.txt");

            _log.AddRange(File.ReadAllLines("log.txt")); PrintLogMessage("Log file loaded", "TRACE");

            Token = Properties.Settings.Default.vkToken; PrintLogMessage("Vk Api Token loaded", "TRACE");

            LoadConfig(); PrintLogMessage("Config from DB loaded", "TRACE");

            var runTeamNameSetup = new Thread(TeamSetup);
            runTeamNameSetup.Start(); PrintLogMessage("Team Setup thread started", "TRACE");

            var playerStatProc = new Thread(PlayerStatisticsProcessing);
            playerStatProc.Start("Logs"); PrintLogMessage("PlayerStatisticsProcessing thread started", "TRACE");

            var cup = new Thread(Cups.Init);
            cup.Start();

            var battleCup = new Thread(BattleCup.Init);
            battleCup.Start(); PrintLogMessage("BattleCup.Init thread started", "TRACE");
        }
    }
}
