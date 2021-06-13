using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace WorkNode
{
    public static class Tools
    {
        public static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            int j;
            var encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        public static void DrawText(Graphics gr, string text, Rectangle rect, StringAlignment alignment, float fontSize, Brush color)
        {
            gr.DrawRectangle(Pens.Blue, rect);
            var font = new Font("Trebuchet MS", fontSize);
            using (var stringFormat = new StringFormat())
            {
                stringFormat.Alignment = alignment;
                stringFormat.FormatFlags = StringFormatFlags.LineLimit;
                stringFormat.Trimming = StringTrimming.Word;
                gr.DrawString(text, font, color, rect, stringFormat);
            }
        }

        public static void DrawTextEx(Graphics gr, string text, Rectangle rect, StringAlignment alignment, float fontSize, Brush color, Font font)
        {
            gr.DrawRectangle(Pens.Blue, rect);
            using (var stringFormat = new StringFormat())
            {
                stringFormat.Alignment = alignment;
                stringFormat.FormatFlags = StringFormatFlags.LineLimit;
                stringFormat.Trimming = StringTrimming.Word;
                gr.DrawString(text, font, color, rect, stringFormat);
            }
        }

        public static string ImagePath(string matchResult)
        {
            if (matchResult == "Победа") return @"Images\Stats\win.jpg";
            else return @"Images\Stats\lose.jpg";
        }

        public static string DefinePlayerRankName(int pts)
        {
            if (pts < 0)
            {
                return "Unknown";
            }
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT Name FROM kTVCSS.ranks WHERE MinPTS <= " + pts + " ORDER BY MinPTS DESC LIMIT 1;", connection);
            var reader = query.ExecuteReader();
            reader.Read();
            var rank = reader[0].ToString();
            connection.Close();
            return rank;
        }

        public static int DefinePlayerRankByIndStats(double kd, double hk)
        {
            var pts = 0;
            if (hk >= 0.4 && hk < 0.5) pts += 350;
            if (hk >= 0.5 && hk < 0.55) pts += 450;
            if (hk >= 0.55 && hk < 0.6) pts += 600;
            if (hk >= 0.6 && hk < 0.65) pts += 700;
            if (hk >= 0.65 && hk < 0.7) pts += 850;
            if (hk >= 0.7) pts += 1000;
            if (kd >= 1 && kd < 1.1) pts += 200;
            if (kd >= 1.1 && kd < 1.2) pts += 300;
            if (kd >= 1.2 && kd < 1.3) pts += 350;
            if (kd >= 1.3 && kd < 1.4) pts += 400;
            if (kd >= 1.4 && kd < 1.5) pts += 450;
            if (kd >= 1.5 && kd < 1.6) pts += 500;
            if (kd >= 1.6 && kd < 1.7) pts += 600;
            if (kd >= 1.7 && kd < 1.8) pts += 700;
            if (kd >= 1.8 && kd < 2.0) pts += 850;
            if (kd >= 2.0) pts += 1000;
            return pts;
        }

        public static string GetMapName(string fileName)
        {
            var lines = File.ReadAllLines(fileName, Encoding.UTF8);
            foreach (var line in lines)
            {
                if (line.Contains("THISMAP"))
                {
                    var map = line.Substring(line.IndexOf("THISMAP"));
                    map = map.Substring(8);
                    map = map.Substring(0, map.IndexOf("\""));
                    return map;
                }
            }
            return "unknown";
        }

        public static int GetPlayerPts(string steamId)
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT RankPTS FROM kTVCSS.players WHERE SteamID = '" + steamId + "';", connection);
            var reader = query.ExecuteReader();
            reader.Read();
            var pts = 0;
            try
            {
                pts = int.Parse(reader[0].ToString());
            }
            catch (Exception)
            {
                // can't read cuz it's 0
            }
            connection.Close();
            return pts;
        }

        public static int DefinePlayerRankPts(string steamId, int pts, bool victory, string team)
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT RankPTS FROM kTVCSS.players WHERE SteamID = '" + steamId + "';", connection);
            var reader = query.ExecuteReader();
            reader.Read();
            pts = int.Parse(reader[0].ToString());
            if (WorkClass.IsNormalMatch(team, pts))
            {
                if (victory)
                {
                    pts += 30;
                }
                else pts -= 30;
                connection.Close();
                return pts;
            }
            if (!victory) pts -= 30;
            connection.Close();
            return pts;
        }

        public static void DefinePlayerRankAfterCalibration()
        {
            var steamId = new List<string>();
            var rankPts = new List<int>();
            var matchesVictories = new List<int>();
            var kd = new List<double>();
            var hk = new List<double>();
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT SteamID, KD, HK, RankPTS, MatchesVictories FROM kTVCSS.players WHERE IsCalibration = '1' AND MatchesPlayed = '10';", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                steamId.Add(reader[0].ToString());
                kd.Add(double.Parse((reader[1]).ToString()));
                hk.Add(double.Parse((reader[2]).ToString()));
                rankPts.Add(int.Parse(reader[3].ToString()));
                matchesVictories.Add(int.Parse(reader[4].ToString()));
            }
            reader.Close();
            connection.Close();
            if (!steamId.Any()) return;
            for (var i = 0; i < steamId.Count(); i++)
            {
                var pts = 0;
                if (matchesVictories[i] == 10)
                {
                    pts = 3500 + rankPts[i];
                }
                if (matchesVictories[i] == 9)
                {
                    pts = 3200 + rankPts[i];
                }
                if (matchesVictories[i] == 8)
                {
                    pts = 3000 + rankPts[i];
                }
                if (matchesVictories[i] == 7)
                {
                    pts = 2700 + rankPts[i];
                }
                if (matchesVictories[i] == 6)
                {
                    pts = 2500 + rankPts[i];
                }
                if (matchesVictories[i] == 5)
                {
                    pts = 2100 + rankPts[i];
                }
                if (matchesVictories[i] == 4)
                {
                    pts = 1900 + rankPts[i];
                }
                if (matchesVictories[i] == 3)
                {
                    pts = 1700 + rankPts[i];
                }
                if (matchesVictories[i] == 2)
                {
                    pts = 1400 + rankPts[i];
                }
                if (matchesVictories[i] == 1)
                {
                    pts = 1000 + rankPts[i];
                }
                if (matchesVictories[i] == 0)
                {
                    pts = 500 + rankPts[i];
                }
                var bonusPts = Tools.DefinePlayerRankByIndStats(kd[i], hk[i]);
                pts += bonusPts;
                connection.Open();
                var rankName = Tools.DefinePlayerRankName(pts);
                var queryUpdate = new MySqlCommand("UPDATE kTVCSS.players SET RankName = '" + rankName + "', RankPTS = '" + pts + "', IsCalibration = '0' WHERE SteamID = '" + steamId[i] + "';", connection);
                queryUpdate.ExecuteNonQuery();
                connection.Close();
            }
        }

        public static int GetLastMatchId()
        {
            var id = 0;
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT Id FROM matches ORDER BY Id DESC LIMIT 1;", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                id = int.Parse(reader[0].ToString());
                id++;
            }
            reader.Close();
            connection.Close();
            return id;
        }

        public static string GetMapImage(string mapName)
        {
            if (mapName.Contains("cache")) return @"Images\\de_cache_csgo.jpg";
            if (mapName.Contains("cbble")) return @"Images\\de_cbble.jpg";
            if (mapName.Contains("de_cpl_fire")) return @"Images\\de_cpl_fire.jpg";
            if (mapName.Contains("mill")) return @"Images\\de_cpl_mill.jpg";
            if (mapName.Contains("strike")) return @"Images\\de_cpl_strike.jpg";
            if (mapName.Contains("dust2")) return @"Images\\de_dust2.jpg";
            if (mapName.Contains("inferno")) return @"Images\\de_inferno.jpg";
            if (mapName.Contains("mirage")) return @"Images\\de_mirage_csgo.jpg";
            if (mapName.Contains("nuke")) return @"Images\\de_nuke.jpg";
            if (mapName.Contains("overpass")) return @"Images\\de_overpass_csgo.jpg";
            if (mapName.Contains("de_russka")) return @"Images\\de_russka.jpg";
            if (mapName.Contains("season")) return @"Images\\de_season.jpg";
            if (mapName.Contains("train")) return @"Images\\de_train_csgo.jpg";
            if (mapName.Contains("tuscan")) return @"Images\\de_tuscan.jpg";
            return "";
        }

        public static string GetRankImage(string rankName)
        {
            if (rankName == "Herald I") return @"Images\Stats\herald1.png";
            if (rankName == "Herald II") return @"Images\Stats\herald2.png";
            if (rankName == "Herald III") return @"Images\Stats\herald3.png";
            if (rankName == "Herald IV") return @"Images\Stats\herald4.png";
            if (rankName == "Herald V") return @"Images\Stats\herald5.png";
            if (rankName == "Guardian I") return @"Images\Stats\guardian1.png";
            if (rankName == "Guardian II") return @"Images\Stats\guardian2.png";
            if (rankName == "Guardian III") return @"Images\Stats\guardian3.png";
            if (rankName == "Guardian IV") return @"Images\Stats\guardian4.png";
            if (rankName == "Guardian V") return @"Images\Stats\guardian5.png";
            if (rankName == "Crusader I") return @"Images\Stats\crusader1.png";
            if (rankName == "Crusader II") return @"Images\Stats\crusader2.png";
            if (rankName == "Crusader III") return @"Images\Stats\crusader3.png";
            if (rankName == "Crusader IV") return @"Images\Stats\crusader4.png";
            if (rankName == "Crusader V") return @"Images\Stats\crusader5.png";
            if (rankName == "Archon I") return @"Images\Stats\archon1.png";
            if (rankName == "Archon II") return @"Images\Stats\archon2.png";
            if (rankName == "Archon III") return @"Images\Stats\archon3.png";
            if (rankName == "Archon IV") return @"Images\Stats\archon4.png";
            if (rankName == "Archon V") return @"Images\Stats\archon5.png";
            if (rankName == "Legend I") return @"Images\Stats\legend1.png";
            if (rankName == "Legend II") return @"Images\Stats\legend2.png";
            if (rankName == "Legend III") return @"Images\Stats\legend3.png";
            if (rankName == "Legend IV") return @"Images\Stats\legend4.png";
            if (rankName == "Legend V") return @"Images\Stats\legend5.png";
            if (rankName == "Ancient I") return @"Images\Stats\ancient1.png";
            if (rankName == "Ancient II") return @"Images\Stats\ancient2.png";
            if (rankName == "Ancient III") return @"Images\Stats\ancient3.png";
            if (rankName == "Ancient IV") return @"Images\Stats\ancient4.png";
            if (rankName == "Ancient V") return @"Images\Stats\ancient5.png";
            if (rankName == "Divine I") return @"Images\Stats\divine1.png";
            if (rankName == "Divine II") return @"Images\Stats\divine2.png";
            if (rankName == "Divine III") return @"Images\Stats\divine3.png";
            if (rankName == "Divine IV") return @"Images\Stats\divine4.png";
            if (rankName == "Divine V") return @"Images\Stats\divine5.png";
            if (rankName == "Titan I") return @"Images\Stats\titan1.png";
            if (rankName == "Titan II") return @"Images\Stats\titan2.png";
            if (rankName == "Titan III") return @"Images\Stats\titan3.png";
            if (rankName == "Titan IV") return @"Images\Stats\titan4.png";
            if (rankName == "Titan V") return @"Images\Stats\titan5.png";
            if (rankName == "Immortal I") return @"Images\Stats\immortal.png";
            return @"Images\Stats\unranked.png";
        }
    }
}
