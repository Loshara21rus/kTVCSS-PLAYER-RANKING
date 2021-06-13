using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
    public static class BattleCup
    {
        public static long BoardId = 0;
        public static int BCupId = 0;
        public static WebClient Web = new WebClient();
        public static List<string> BTeams = new List<string>();
        public static Random Random = new Random();
        public static List<string> Teams = new List<string>();
        public static long PostId = 0;
        public static bool CupActive = true;
        private static int _status = 0;
        public static int Password = 0;

        private static string GetDataFromRecovery(string paramName)
        {
            string toReturn = null;
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT " + paramName + " FROM battlecup_recovery;", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                toReturn = reader[0].ToString();
            }
            connection.Close();
            return toReturn;
        }

        private static void InsertDataToRecovery(string paramName, string value)
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("UPDATE battlecup_recovery SET " + paramName + " = '" + value + "' WHERE " + paramName + " = '0';", connection);
            query.ExecuteNonQuery();
            connection.Close();
        }

        private static bool BeginBattleCup()
        {
            if ((DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday) && DateTime.Now.Hour == 12 && DateTime.Now.Minute == 0) return true;
            else return false;
        }

        private static int Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                WorkClass.PrintLogMessage("battlecup status changed to " + value, "TRACE");
            }
        }

        public static void Init()
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT Status FROM battlecup_list WHERE Status != 0;", connection);
            var reader = query.ExecuteReader();
            var isAlreadyCup = reader.HasRows;

            while (reader.Read())
            {
                Status = int.Parse(reader[0].ToString());
            }
            connection.Close();
            if (!isAlreadyCup)
            {
                while (!BeginBattleCup())
                {
                    Thread.Sleep(30000);
                }
                StartRegistration(-1);
            }
            else
            {
                PostId = long.Parse(GetDataFromRecovery("postId"));
                BCupId = int.Parse(GetDataFromRecovery("bCupId"));
                BoardId = long.Parse(GetDataFromRecovery("RegBoardId"));
                Password = int.Parse(GetDataFromRecovery("password"));

                var tempBTeamsRow = GetDataFromRecovery("bTeams");
                if (tempBTeamsRow != "0")
                {
                    BTeams.AddRange(tempBTeamsRow.Split(';'));
                }
                var tempTeamsRow = GetDataFromRecovery("Teams");
                if (tempTeamsRow != "0")
                {
                    Teams.AddRange(tempTeamsRow.Split(';'));
                }

                if (Status >= 1 && Status <= 3)
                {
                    StartRegistration(Status);
                }
                if (Status >= 4 && Status <= 5)
                {
                    GetFullTeamCount(Status);
                }
                if (Status == 6)
                {
                    ShuffleTeams(Status);
                }
                if (Status >= 7 && Status <= 10)
                {
                    MakeSchedule(Status);
                }
                if (Status >= 11 && Status <= 19)
                {
                    KickVegetables(Status);
                }
            }
        }

        private static void UpdateCupStatus(string battleCupName, int statusId)
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("UPDATE battlecup_list SET Status = '" + statusId + "' WHERE Name = '" + battleCupName + "';", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                BCupId = int.Parse(reader[0].ToString().Substring(14)) + 1;
            }
            connection.Close();
            Status = statusId;
        }

        private static void StartRegistration(int recoverId)
        {
            var api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = WorkClass.Token,
            });
            var uploadServer = api.Photo.GetWallUploadServer(WorkClass.MainGroupId);
            var result = Encoding.ASCII.GetString(Web.UploadFile(uploadServer.UploadUrl, @"Images\BattleCup\StartReg.jpg"));
            var photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.MainGroupId);
            if (recoverId != -1)
            {
                switch (recoverId)
                {
                    case 1:
                        {
                            goto CupStatusId1;
                        }
                    case 2:
                        {
                            goto CupStatusId2;
                        }
                    case 3:
                        {
                            goto CupStatusId3;
                        }
                }
            }
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT Name FROM battlecup_list ORDER BY Id DESC LIMIT 1;", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                BCupId = int.Parse(reader[0].ToString().Substring(14)) + 1;
            }
            reader.Close();
            query = new MySqlCommand("INSERT INTO battlecup_list (`Name`, `Bracket`, `Status`) VALUES ('" + "Боевой кубок №" + BCupId + "', '8', '1')", connection);
            query.ExecuteNonQuery();
            connection.Close();
            InsertDataToRecovery("bCupId", BCupId.ToString());
            UpdateCupStatus("Боевой кубок №" + BCupId, 1);
        CupStatusId1:
            BoardId = api.Board.AddTopic(new BoardAddTopicParams
            {
                GroupId = WorkClass.MainGroupId,
                FromGroup = true,
                Text = "Боевой кубок - это однодневный фасткап на 8 команд. Поскольку это микс турнир, поэтому в заявке на участие необязательно указывать состав команды. Необходимо лишь указать название команды.\r\n\r\nВАЖНО: Результаты матчей не будут учитываться, если клантег в игре не будет совпадать с тем, который Вы укажете в заявке. Например, если Ваша команда называется Syndikat Expert, но в игре у Вас тег SE, то в заявке нужно указывать SE.",
                Title = "Боевой кубок №" + BCupId,
                Attachments = new List<MediaAttachment>
                {
                    photo.FirstOrDefault()
                }
            });
            InsertDataToRecovery("RegBoardId", BoardId.ToString());
            UpdateCupStatus("Боевой кубок №" + BCupId, 2);
        CupStatusId2:
            api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = "Открыта регистрация на Боевой кубок №" + BCupId + "\r\nРегистрация: https://vk.com/topic-" + WorkClass.MainGroupId + "_" + BoardId + "\r\nБоевой кубок - это однодневный фасткап на 8 команд. Поскольку это микс турнир, поэтому в заявке на участие необязательно указывать состав команды. Необходимо лишь указать название команды. Начало турнира в 18:00, полуфиналы в 19:00 и финал в 20:00.",
                FromGroup = true,
                Signed = false,
                Attachments = new List<MediaAttachment>
                {
                    photo.FirstOrDefault()
                }
            });
            UpdateCupStatus("Боевой кубок №" + BCupId, 3);
        CupStatusId3:
            GetFullTeamCount(3);
        }

        private static void GetFullTeamCount(int recoverId)
        {
            var api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = WorkClass.Token,
            });
            var messages = api.Board.GetComments(new BoardGetCommentsParams()
            {
                GroupId = WorkClass.MainGroupId,
                TopicId = BoardId,
                Count = 9
            });
            if (recoverId != 3)
            {
                switch (recoverId)
                {
                    case 4:
                        {
                            goto CupStatusId4;
                        }
                    case 5:
                        {
                            goto CupStatusId5;
                        }
                }
            }
            while (messages.Count < 9)
            {
                messages = api.Board.GetComments(new BoardGetCommentsParams()
                {
                    GroupId = WorkClass.MainGroupId,
                    TopicId = BoardId,
                    Count = 9
                });
                Thread.Sleep(60 * 1000);
            }
            UpdateCupStatus("Боевой кубок №" + BCupId, 4);
        CupStatusId4:
            for (int i = 1; i < messages.Items.Count; i++)
            {
                try
                {
                    BTeams.Add(messages.Items[i].Text.Split('\n')[0].Split(' ')[0]);
                }
                catch (ArgumentOutOfRangeException)
                {
                    BTeams.Add(messages.Items[i].Text);
                };
            }
            var bTeamsRow = "";
            foreach (var team in BTeams)
            {
                bTeamsRow += team + ";";
            }
            bTeamsRow = bTeamsRow.Remove(bTeamsRow.Length - 1, 1);
            InsertDataToRecovery("bTeams", bTeamsRow);
            UpdateCupStatus("Боевой кубок №" + BCupId, 5);
        CupStatusId5:
            ShuffleTeams(5);
        }

        private static void ShuffleTeams(int recoverId)
        {
            if (recoverId != 5)
            {
                switch (recoverId)
                {
                    case 6:
                        {
                            goto CupStatusId6;
                        }
                }
            }
            foreach (var s in BTeams)
            {
                int j = Random.Next(Teams.Count() + 1);
                if (j == Teams.Count)
                {
                    Teams.Add(s);
                }
                else
                {
                    Teams.Add(Teams[j]);
                    Teams[j] = s;
                }
            }
            var teamsRow = "";
            foreach (var team in Teams)
            {
                teamsRow += team + ";";
            }
            teamsRow = teamsRow.Remove(teamsRow.Length - 1, 1);
            InsertDataToRecovery("Teams", teamsRow);
            UpdateCupStatus("Боевой кубок №" + BCupId, 6);
        CupStatusId6:
            MakeSchedule(6);
        }

        private static void MakeSchedule(int recoverId)
        {
            var api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = WorkClass.Token,
            });
            if (recoverId != 6)
            {
                switch (recoverId)
                {
                    case 7:
                        {
                            goto CupStatusId7;
                        }
                    case 8:
                        {
                            goto CupStatusId8;
                        }
                    case 9:
                        {
                            goto CupStatusId9;
                        }
                    case 10:
                        {
                            goto CupStatusId10;
                        }
                }
            }
            UpdateCupStatus("Боевой кубок №" + BCupId, 7);
        CupStatusId7:

            var bracketStyle = new Random().Next(1, 7);
            var image = System.Drawing.Image.FromFile(@"Images\BattleCup\" + bracketStyle + ".png");
            var graphics = Graphics.FromImage(image);
            var font = new Font("FRABK", 18);
            graphics.DrawString(Teams[0].ToUpper(), font, Brushes.White, 105, 150);
            graphics.DrawString(Teams[1].ToUpper(), font, Brushes.White, 105, 195);
            graphics.DrawString(Teams[2].ToUpper(), font, Brushes.White, 105, 250);
            graphics.DrawString(Teams[3].ToUpper(), font, Brushes.White, 105, 295);
            graphics.DrawString(Teams[4].ToUpper(), font, Brushes.White, 105, 388);
            graphics.DrawString(Teams[5].ToUpper(), font, Brushes.White, 105, 430);
            graphics.DrawString(Teams[6].ToUpper(), font, Brushes.White, 105, 485);
            graphics.DrawString(Teams[7].ToUpper(), font, Brushes.White, 105, 530);
            graphics.DrawString(BCupId.ToString(), new Font("FRABKIT", 48), Brushes.White, 900, 75);
            image.Save(@"Images\BattleCup\bracket_temp.png", System.Drawing.Imaging.ImageFormat.Png);

            var uploadServer = api.Photo.GetWallUploadServer(WorkClass.MainGroupId);
            var result = Encoding.ASCII.GetString(Web.UploadFile(uploadServer.UploadUrl, @"Images\BattleCup\bracket_temp.png"));
            var photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.MainGroupId);

            PostId = api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = "Расписание на Боевой кубок №" + BCupId + " (" + DateTime.Now.ToString("dd.MM.yyyy") + ")\r\n" + Teams[0] + " vs " + Teams[1] + " - 18:00 @ bo1 (kTVCSS №1)\r\n" + Teams[2] + " vs " + Teams[3] + " - 18:00 @ bo1 (kTVCSS №2)\r\n" + Teams[4] + " vs " + Teams[5] + " - 18:00 @ bo1 (kTVCSS №3)\r\n" + Teams[6] + " vs " + Teams[7] + " - 18:00 @ bo1 (kTVCSS №4)\r\nПравила турниров: https://v34.ktvcss.org.ru/rules.php\r\nПо любым вопросам, относящимся к БК, обращаться к @waneda_avganskaya(Waneda).\r\nУбедительная просьба не начинать полуфинальные матчи до публикации результатов четверть-финала (особенно на 1 и 2 серверах, ибо в момент публикации расписания на этих серверах будет отменен матч и будут кикнуты все игроки, поэтому если вы сильно хотите начать пораньше, то выбирайте 3 или 4 сервера).",
                FromGroup = true,
                Signed = false,
                Attachments = new List<MediaAttachment>
                {
                    photo.FirstOrDefault()
                }
            });
            InsertDataToRecovery("postId", PostId.ToString());
            UpdateCupStatus("Боевой кубок №" + BCupId, 8);
        CupStatusId8:
            Password = Random.Next(1000, 2000);
            InsertDataToRecovery("password", Password.ToString());
            var messages = api.Board.GetComments(new BoardGetCommentsParams()
            {
                GroupId = WorkClass.MainGroupId,
                TopicId = BoardId,
                Count = 9
            });
            for (int i = 1; i < messages.Items.Count; i++)
            {
                try
                {
                    api.Messages.Send(new MessagesSendParams()
                    {
                        GroupId = (ulong)WorkClass.MainGroupId,
                        PeerId = messages.Items[i].FromId,
                        Message = "Здравствуйте! Вы участвуете в боевом кубке №" + BCupId + "!\r\nСсылка на расписание матчей: https://vk.com/ktvcss?w=wall-" + WorkClass.MainGroupId + "_" + PostId + "\r\nПароль от серверов на весь турнир: " + Password.ToString(),
                        RandomId = new Random().Next()
                    });
                }
                catch (Exception)
                {
                    try
                    {
                        api.Messages.Send(new MessagesSendParams()
                        {
                            PeerId = messages.Items[i].FromId,
                            Message = "Здравствуйте! Вы участвуете в боевом кубке №" + BCupId + "!\r\nСсылка на расписание матчей: https://vk.com/ktvcss?w=wall-" + WorkClass.MainGroupId + "_" + PostId + "\r\nПароль от серверов на весь турнир: " + Password.ToString(),
                            RandomId = new Random().Next()
                        });
                    }
                    catch (Exception)
                    {
                        // Ignored
                    }
                }
            }
            UpdateCupStatus("Боевой кубок №" + BCupId, 9);
        CupStatusId9:
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("INSERT INTO battlecup_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`) VALUES ('" + Teams[0] + "', '" + Teams[1] + "', '" + BCupId + "', '0', '" + DateTime.Now.ToString("yyyy-MM-dd") + " 18:00:00', '" + DateTime.Now.ToString("yyyy-MM-dd") + " 19:30:00', '1/4');", connection);
            query.ExecuteNonQuery();
            query = new MySqlCommand("INSERT INTO battlecup_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`) VALUES ('" + Teams[2] + "', '" + Teams[3] + "', '" + BCupId + "', '1', '" + DateTime.Now.ToString("yyyy-MM-dd") + " 18:00:00', '" + DateTime.Now.ToString("yyyy-MM-dd") + " 19:30:00', '1/4');", connection);
            query.ExecuteNonQuery();
            query = new MySqlCommand("INSERT INTO battlecup_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`) VALUES ('" + Teams[4] + "', '" + Teams[5] + "', '" + BCupId + "', '2', '" + DateTime.Now.ToString("yyyy-MM-dd") + " 18:00:00', '" + DateTime.Now.ToString("yyyy-MM-dd") + " 19:30:00', '1/4');", connection);
            query.ExecuteNonQuery();
            query = new MySqlCommand("INSERT INTO battlecup_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`) VALUES ('" + Teams[6] + "', '" + Teams[7] + "', '" + BCupId + "', '3', '" + DateTime.Now.ToString("yyyy-MM-dd") + " 18:00:00', '" + DateTime.Now.ToString("yyyy-MM-dd") + " 19:30:00', '1/4');", connection);
            query.ExecuteNonQuery();
            connection.Close();
            UpdateCupStatus("Боевой кубок №" + BCupId, 10);
        CupStatusId10:
            KickVegetables(10);
        }

        private static bool IsTimeToKick()
        {
            if ((DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday) && DateTime.Now.Hour == 17 && DateTime.Now.Minute == 55) return true;
            else return false;
        }

        private static void KickFromServer(string host, string port, string rconPassword)
        {
            var webClient = new WebClient();
            try
            {
                webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host + "&port=" + port + "&password=" + rconPassword + "&command=say [BattleCupBot] Через 5 минут начинается боевой кубок на этом сервере");
                Thread.Sleep(1000);
                webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host + "&port=" + port + "&password=" + rconPassword + "&command=say [BattleCupBot] Приносим извинения, если вы не успели доиграть матч");
                Thread.Sleep(3000);
                webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host + "&port=" + port + "&password=" + rconPassword + "&command=cm");
                Thread.Sleep(1000);
                webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host + "&port=" + port + "&password=" + rconPassword + "&command=sm_kick @ct There is the BattleCup now!");
                webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host + "&port=" + port + "&password=" + rconPassword + "&command=sm_kick @t There is the BattleCup now!");
                Thread.Sleep(1000);
                webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host + "&port=" + port + "&password=" + rconPassword + "&command=d2");
                Thread.Sleep(10000);
                webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host + "&port=" + port + "&password=" + rconPassword + "&command=sv_password " + Password.ToString());
            }
            catch (Exception ex)
            {
                WorkClass.PrintLogMessage(ex.ToString(), "ERROR");
            }
        }

        private static void KickVegetables(int recoverId)
        {
            var api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = WorkClass.Token,
            });
            if (recoverId != 10)
            {
                switch (recoverId)
                {
                    case 11:
                        {
                            goto CupStatusId11;
                        }
                    case 12:
                        {
                            goto CupStatusId12;
                        }
                    case 13:
                        {
                            goto CupStatusId13;
                        }
                    case 14:
                        {
                            goto CupStatusId14;
                        }
                    case 15:
                        {
                            goto CupStatusId15;
                        }
                    case 16:
                        {
                            goto CupStatusId16;
                        }
                    case 17:
                        {
                            goto CupStatusId17;
                        }
                    case 18:
                        {
                            goto CupStatusId18;
                        }
                    case 19:
                        {
                            goto CupStatusId19;
                        }
                }
            }
            while (!IsTimeToKick())
            {
                Thread.Sleep(30000);
            }
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT ServerId FROM battlecup_matches WHERE BracketItem = '1/4' AND TournamentId = '" + BCupId + "';", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                KickFromServer(WorkClass.ServerList[int.Parse(reader[0].ToString())].Host, WorkClass.ServerList[int.Parse(reader[0].ToString())].GamePort.ToString(), WorkClass.ServerList[int.Parse(reader[0].ToString())].RconPassword);
            }
            connection.Close();
            UpdateCupStatus("Боевой кубок №" + BCupId, 11);
        CupStatusId11:
            CupActive = true;
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId, TeamAName, TeamBName, DateTimeStart, DateTimeEnd FROM battlecup_matches WHERE BracketItem = '1/4' AND TournamentId = '" + BCupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                var checkThreadQuarterFinal = new Thread(CheckTrueTeamNames);
                checkThreadQuarterFinal.Start(reader[0].ToString() + ";" + reader[1].ToString() + ";" + reader[2].ToString() + ";" + reader[3].ToString() + ";" + reader[4].ToString() + ";1/4");
            }
            connection.Close();
            while (!CheckIsPartFinished(4, "1/4"))
            {
                Thread.Sleep(15000);
            }
            UpdateCupStatus("Боевой кубок №" + BCupId, 12);
        CupStatusId12:
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var winnersQuarters = new List<string>();
            query = new MySqlCommand("SELECT MatchWinnerName FROM battlecup_matches WHERE BracketItem = '1/4' AND TournamentId = '" + BCupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                winnersQuarters.Add(reader[0].ToString());
            }
            reader.Close();
            query = new MySqlCommand("INSERT INTO battlecup_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`) VALUES ('" + winnersQuarters[0] + "', '" + winnersQuarters[1] + "', '" + BCupId + "', '0', '" + DateTime.Now.ToString("yyyy-MM-dd") + " 19:15:00', '" + DateTime.Now.ToString("yyyy-MM-dd") + " 20:45:00', '1/2');", connection);
            query.ExecuteNonQuery();
            query = new MySqlCommand("INSERT INTO battlecup_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`) VALUES ('" + winnersQuarters[2] + "', '" + winnersQuarters[3] + "', '" + BCupId + "', '1', '" + DateTime.Now.ToString("yyyy-MM-dd") + " 19:15:00', '" + DateTime.Now.ToString("yyyy-MM-dd") + " 20:45:00', '1/2');", connection);
            query.ExecuteNonQuery();
            connection.Close();
            UpdateCupStatus("Боевой кубок №" + BCupId, 13);
        CupStatusId13:
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            winnersQuarters = new List<string>();
            var matchesToPrint = new List<string>();
            query = new MySqlCommand("SELECT MatchWinnerName, MatchId FROM battlecup_matches WHERE BracketItem = '1/4' AND TournamentId = '" + BCupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                winnersQuarters.Add(reader[0].ToString());
                matchesToPrint.Add(reader[1].ToString());
            }
            reader.Close();
            connection.Close();
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var firstResultPost = "";
            var rez = "";
            foreach (var matchId in matchesToPrint)
            {
                query = new MySqlCommand("SELECT TeamA, TeamAScore, TeamB, TeamBScore, Map FROM matches WHERE ID = '" + matchId + "';", connection);
                reader = query.ExecuteReader();
                while (reader.Read())
                {
                    firstResultPost += reader[0].ToString() + " [" + reader[1].ToString() + "-" + reader[3].ToString() + "] " + reader[2].ToString() + " @ " + reader[4].ToString() + "\r\n";

                    if (reader[0].ToString() == Teams[0] && reader[2].ToString() == Teams[1])
                    {
                        rez += reader[1].ToString() + ";" + reader[3].ToString() + ";";
                        continue;
                    }

                    if (reader[0].ToString() == Teams[1] && reader[2].ToString() == Teams[0])
                    {
                        rez += reader[3].ToString() + ";" + reader[1].ToString() + ";";
                        continue;
                    }

                    if (reader[0].ToString() == Teams[2] && reader[2].ToString() == Teams[3])
                    {
                        rez += reader[1].ToString() + ";" + reader[3].ToString() + ";";
                        continue;
                    }

                    if (reader[0].ToString() == Teams[3] && reader[2].ToString() == Teams[2])
                    {
                        rez += reader[3].ToString() + ";" + reader[1].ToString() + ";";
                        continue;
                    }

                    if (reader[0].ToString() == Teams[4] && reader[2].ToString() == Teams[5])
                    {
                        rez += reader[1].ToString() + ";" + reader[3].ToString() + ";";
                        continue;
                    }

                    if (reader[0].ToString() == Teams[5] && reader[2].ToString() == Teams[4])
                    {
                        rez += reader[3].ToString() + ";" + reader[1].ToString() + ";";
                        continue;
                    }

                    if (reader[0].ToString() == Teams[6] && reader[2].ToString() == Teams[7])
                    {
                        rez += reader[1].ToString() + ";" + reader[3].ToString() + ";";
                        continue;
                    }

                    if (reader[0].ToString() == Teams[7] && reader[2].ToString() == Teams[6])
                    {
                        rez += reader[3].ToString() + ";" + reader[1].ToString() + ";";
                        continue;
                    }
                }
                reader.Close();
            }
            connection.Close();
            var rezs = rez.Split(';');

            var image = System.Drawing.Image.FromFile(@"Images\BattleCup\bracket_temp.png");
            var graphics = Graphics.FromImage(image);
            var font = new Font("FRABK", 18);

            graphics.DrawString(rezs[0].ToUpper(), new Font("AGENCYR", 18), Brushes.White, 280, 150);
            graphics.DrawString(rezs[1].ToUpper(), new Font("AGENCYR", 18), Brushes.White, 280, 195);
            graphics.DrawString(rezs[2].ToUpper(), new Font("AGENCYR", 18), Brushes.White, 280, 250);
            graphics.DrawString(rezs[3].ToUpper(), new Font("AGENCYR", 18), Brushes.White, 280, 295);
            graphics.DrawString(rezs[4].ToUpper(), new Font("AGENCYR", 18), Brushes.White, 280, 388);
            graphics.DrawString(rezs[5].ToUpper(), new Font("AGENCYR", 18), Brushes.White, 280, 430);
            graphics.DrawString(rezs[6].ToUpper(), new Font("AGENCYR", 18), Brushes.White, 280, 485);
            graphics.DrawString(rezs[7].ToUpper(), new Font("AGENCYR", 18), Brushes.White, 280, 530);

            graphics.DrawString(winnersQuarters[0].ToUpper(), font, Brushes.White, 435, 200);
            graphics.DrawString(winnersQuarters[1].ToUpper(), font, Brushes.White, 435, 245);
            graphics.DrawString(winnersQuarters[2].ToUpper(), font, Brushes.White, 435, 437);
            graphics.DrawString(winnersQuarters[3].ToUpper(), font, Brushes.White, 435, 480);
            image.Save(@"Images\BattleCup\bracket_temp2.png", ImageFormat.Png);

            var uploadServer = api.Photo.GetWallUploadServer(WorkClass.MainGroupId);
            var result = Encoding.ASCII.GetString(Web.UploadFile(uploadServer.UploadUrl, @"Images\BattleCup\bracket_temp2.png"));
            var photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.MainGroupId);

            PostId = api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = "Расписание на Боевой кубок №" + BCupId + " (" + DateTime.Now.ToString("dd.MM.yyyy") + ")\r\n" + winnersQuarters[0] + " vs " + winnersQuarters[1] + " - 19:15 @ bo1 (kTVCSS №1)\r\n" + winnersQuarters[2] + " vs " + winnersQuarters[3] + " - 19:15 @ bo1 (kTVCSS №2)\r\nРезультаты четверть-финала:\r\n" + firstResultPost + "Правила турниров: https://v34.ktvcss.org.ru/rules.php",
                FromGroup = true,
                Signed = false,
                Attachments = new List<MediaAttachment>
                {
                    photo.FirstOrDefault()
                }
            });

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId FROM battlecup_matches WHERE BracketItem = '1/2' AND TournamentId = '" + BCupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                KickFromServer(WorkClass.ServerList[int.Parse(reader[0].ToString())].Host, WorkClass.ServerList[int.Parse(reader[0].ToString())].GamePort.ToString(), WorkClass.ServerList[int.Parse(reader[0].ToString())].RconPassword);
            }
            connection.Close();

            UpdateCupStatus("Боевой кубок №" + BCupId, 14);
        CupStatusId14:
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId, TeamAName, TeamBName, DateTimeStart, DateTimeEnd FROM battlecup_matches WHERE BracketItem = '1/2' AND TournamentId = '" + BCupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                var checkThreadQuarterFinal = new Thread(CheckTrueTeamNames);
                checkThreadQuarterFinal.Start(reader[0].ToString() + ";" + reader[1].ToString() + ";" + reader[2].ToString() + ";" + reader[3].ToString() + ";" + reader[4].ToString() + ";1/2");
            }
            connection.Close();
            while (!CheckIsPartFinished(2, "1/2"))
            {
                Thread.Sleep(15000);
            }
            UpdateCupStatus("Боевой кубок №" + BCupId, 15);
        CupStatusId15:
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            winnersQuarters = new List<string>();
            query = new MySqlCommand("SELECT MatchWinnerName FROM battlecup_matches WHERE BracketItem = '1/2' AND TournamentId = '" + BCupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                winnersQuarters.Add(reader[0].ToString());
            }
            reader.Close();
            query = new MySqlCommand("INSERT INTO battlecup_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`) VALUES ('" + winnersQuarters[0] + "', '" + winnersQuarters[1] + "', '" + BCupId + "', '0', '" + DateTime.Now.ToString("yyyy-MM-dd") + " 20:20:00', '" + DateTime.Now.ToString("yyyy-MM-dd") + " 23:50:00', '1/1');", connection);
            query.ExecuteNonQuery();
            connection.Close();
            UpdateCupStatus("Боевой кубок №" + BCupId, 16);
        CupStatusId16:
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            winnersQuarters = new List<string>();
            query = new MySqlCommand("SELECT MatchWinnerName FROM battlecup_matches WHERE BracketItem = '1/2' AND TournamentId = '" + BCupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                winnersQuarters.Add(reader[0].ToString());
            }
            reader.Close();
            connection.Close();

            var previosWinners = new List<string>();
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();

            query = new MySqlCommand("SELECT MatchWinnerName FROM battlecup_matches WHERE BracketItem = '1/4' AND TournamentId = '" + BCupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                previosWinners.Add(reader[0].ToString());
            }
            reader.Close();
            connection.Close();

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            matchesToPrint = new List<string>();
            query = new MySqlCommand("SELECT MatchId FROM battlecup_matches WHERE BracketItem = '1/2' AND TournamentId = '" + BCupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                matchesToPrint.Add(reader[0].ToString());
            }
            reader.Close();
            var secondResultPost = "";
            rez = "";
            foreach (var matchId in matchesToPrint)
            {
                query = new MySqlCommand("SELECT TeamA, TeamAScore, TeamB, TeamBScore, Map FROM matches WHERE ID = '" + matchId + "';", connection);
                reader = query.ExecuteReader();
                while (reader.Read())
                {
                    secondResultPost += reader[0].ToString() + " [" + reader[1].ToString() + "-" + reader[3].ToString() + "] " + reader[2].ToString() + " @ " + reader[4].ToString() + "\r\n";

                    // new

                    if (previosWinners[0] == reader[0].ToString() && previosWinners[1] == reader[2].ToString())
                    {
                        rez += reader[1].ToString() + ";" + reader[3].ToString() + ";";
                        continue;
                    }
                    if (previosWinners[1] == reader[0].ToString() && previosWinners[0] == reader[2].ToString())
                    {
                        rez += reader[3].ToString() + ";" + reader[1].ToString() + ";";
                        continue;
                    }

                    if (previosWinners[2] == reader[0].ToString() && previosWinners[3] == reader[2].ToString())
                    {
                        rez += reader[1].ToString() + ";" + reader[3].ToString() + ";";
                        continue;
                    }
                    if (previosWinners[3] == reader[0].ToString() && previosWinners[2] == reader[2].ToString())
                    {
                        rez += reader[3].ToString() + ";" + reader[1].ToString() + ";";
                        continue;
                    }
                }
                reader.Close();
            }
            connection.Close();
            rezs = rez.Split(';');

            image = System.Drawing.Image.FromFile(@"Images\BattleCup\bracket_temp2.png");
            graphics = Graphics.FromImage(image);
            font = new Font("FRABK", 18);

            graphics.DrawString(rezs[0].ToUpper(), new Font("AGENCYR", 18), Brushes.White, 600, 200);
            graphics.DrawString(rezs[1].ToUpper(), new Font("AGENCYR", 18), Brushes.White, 600, 245);
            graphics.DrawString(rezs[2].ToUpper(), new Font("AGENCYR", 18), Brushes.White, 600, 437);
            graphics.DrawString(rezs[3].ToUpper(), new Font("AGENCYR", 18), Brushes.White, 600, 480);

            graphics.DrawString(winnersQuarters[0].ToUpper(), font, Brushes.White, 785, 318);
            graphics.DrawString(winnersQuarters[1].ToUpper(), font, Brushes.White, 785, 360);
            image.Save(@"Images\BattleCup\bracket_temp3.png", ImageFormat.Png);

            uploadServer = api.Photo.GetWallUploadServer(WorkClass.MainGroupId);
            result = Encoding.ASCII.GetString(Web.UploadFile(uploadServer.UploadUrl, @"Images\BattleCup\bracket_temp3.png"));
            photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.MainGroupId);

            PostId = api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = "Расписание на Боевой кубок №" + BCupId + " (" + DateTime.Now.ToString("dd.MM.yyyy") + ")\r\n" + winnersQuarters[0] + " vs " + winnersQuarters[1] + " - 20:20 @ bo1 (kTVCSS №1)\r\nРезультаты полуфиналов:\r\n" + secondResultPost + "Правила турниров: https://v34.ktvcss.org.ru/rules.php",
                FromGroup = true,
                Signed = false,
                Attachments = new List<MediaAttachment>
                {
                    photo.FirstOrDefault()
                }
            });

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId FROM battlecup_matches WHERE BracketItem = '1/1' AND TournamentId = '" + BCupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                KickFromServer(WorkClass.ServerList[int.Parse(reader[0].ToString())].Host, WorkClass.ServerList[int.Parse(reader[0].ToString())].GamePort.ToString(), WorkClass.ServerList[int.Parse(reader[0].ToString())].RconPassword);
            }
            connection.Close();

            UpdateCupStatus("Боевой кубок №" + BCupId, 17);
        CupStatusId17:
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId, TeamAName, TeamBName, DateTimeStart, DateTimeEnd FROM battlecup_matches WHERE BracketItem = '1/1' AND TournamentId = '" + BCupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                var checkThreadQuarterFinal = new Thread(CheckTrueTeamNames);
                checkThreadQuarterFinal.Start(reader[0].ToString() + ";" + reader[1].ToString() + ";" + reader[2].ToString() + ";" + reader[3].ToString() + ";" + reader[4].ToString() + ";1/1");
            }
            connection.Close();
            while (!CheckIsPartFinished(1, "1/1"))
            {
                Thread.Sleep(15000);
            }
            UpdateCupStatus("Боевой кубок №" + BCupId, 18);
        CupStatusId18:
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            winnersQuarters = new List<string>();
            var mId = 0;
            query = new MySqlCommand("SELECT MatchWinnerName, MatchId FROM battlecup_matches WHERE BracketItem = '1/1' AND TournamentId = '" + BCupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                winnersQuarters.Add(reader[0].ToString());
                mId = int.Parse(reader[1].ToString());
            }
            connection.Close();
            var simage = System.Drawing.Image.FromFile(@"Images\BattleCup\gramota.jpg");
            var sgraphics = Graphics.FromImage(simage);
            var srectangleC = new Rectangle(500, 600, 0, 200);
            var fontSize = 0;
            if (winnersQuarters[0].ToUpper().Length < 9) fontSize = 128;
            else fontSize = 96;
            Tools.DrawTextEx(sgraphics, winnersQuarters[0].ToUpper(), srectangleC, StringAlignment.Center, 128, Brushes.White, new Font("FRABK", fontSize, FontStyle.Bold));
            sgraphics.DrawString(BCupId.ToString(), new Font("FRABK", 64, FontStyle.Bold), Brushes.White, 755, 473);
            simage.Save(@"Images\BattleCup\gramota_temp.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            uploadServer = api.Photo.GetWallUploadServer(WorkClass.MainGroupId);
            result = Encoding.ASCII.GetString(Web.UploadFile(uploadServer.UploadUrl, @"Images\BattleCup\gramota_temp.jpg"));
            photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.MainGroupId);
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var resultString = "";
            winnersQuarters = new List<string>();
            query = new MySqlCommand("SELECT TeamA, TeamAScore, TeamB, TeamBScore, Map FROM matches WHERE ID = '" + mId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                resultString = reader[0].ToString() + " [" + reader[1].ToString() + "-" + reader[3].ToString() + "] " + reader[2].ToString() + " @ " + reader[4].ToString();
            }
            connection.Close();
            PostId = api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = "Боевой кубок №" + BCupId + " @ Результаты финала\r\n" + resultString,
                FromGroup = true,
                Signed = false,
                Attachments = new List<MediaAttachment>
                {
                    photo.FirstOrDefault()
                }
            });
            UpdateCupStatus("Боевой кубок №" + BCupId, 19);
        CupStatusId19:
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("UPDATE battlecup_list SET Status = '0' WHERE Name = 'Боевой кубок №" + BCupId + "';", connection);
            query.ExecuteNonQuery();
            query = new MySqlCommand("UPDATE battlecup_recovery SET RegBoardId = '0', bCupId = '0', bTeams = '0', Teams = '0', postId = '0', password = '0';", connection);
            query.ExecuteNonQuery();
            query = new MySqlCommand("TRUNCATE TABLE battlecup_matches", connection);
            query.ExecuteNonQuery();
            try
            {
                api.Board.DeleteTopic(new BoardTopicParams()
                {
                    GroupId = WorkClass.MainGroupId,
                    TopicId = BoardId
                });
            }
            catch (Exception)
            {
                //
            }
            WorkClass.PrintLogMessage("battlecup has been ended!", "DEBUG");
        }

        private static bool CheckIsPartFinished(int playCountNeedle, string partName)
        {
            var playCount = 0;
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT MatchPlayed FROM battlecup_matches WHERE TournamentId = '" + BCupId + "' AND BracketItem = '" + partName.ToString() + "';", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                if (reader[0].ToString() == "1") playCount++;
            }
            connection.Close();
            if (playCount == playCountNeedle) return true;
            else return false;
        }

        private static void CheckTrueTeamNames(object parameters)
        {
            var input = parameters.ToString().Split(';');
            var serverId = int.Parse(input[0]);
            var tTrueName = input[1];
            var ctTrueName = input[2];
            var dateFrom = input[3];
            var dateTo = input[4];
            var tPart = input[5];
            var webClient = new WebClient()
            {
                Encoding = Encoding.UTF8
            };
            while (CupActive)
            {
                var tRequest = webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=clientmod_team_t");
                var tName = new Regex(@"(<TERRORISTS>)(.*)("" [(] def. """" [)])", RegexOptions.IgnoreCase);
                var tRegex = tName.Match(tRequest).Groups[2].Value;

                var ctRequest = webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=clientmod_team_ct");
                var ctName = new Regex(@"(<CT>)(.*)("" [(] def. """" [)])", RegexOptions.IgnoreCase);
                var ctRegex = ctName.Match(ctRequest).Groups[2].Value;

                if (string.IsNullOrEmpty(tRegex) || string.IsNullOrEmpty(ctRegex)) goto EmptyTag;

                if ((tRegex.ToString().Contains(tTrueName) || ctRegex.ToString().Contains(ctTrueName)) || (tRegex.ToString().Contains(ctTrueName) || ctRegex.ToString().Contains(tTrueName)))
                {
                    if (!(tRegex.ToString().Contains("MixTeam") && !ctRegex.ToString().Contains("MixTeam")))
                    {
                        // OK  
                    }
                    else
                    {
                        WorkClass.PrintLogMessage("INCORRECT TAGS " + tRegex + " - " + tTrueName + "; " + ctRegex + " - " + ctTrueName, "DEBUG");
                        webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=say [BattleCupBot] Пожалуйста, поставьте правильные теги команд, иначе результат матча не будет учтен");
                        Thread.Sleep(500);
                        webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=say [BattleCupBot] Правильный тег команды А: " + tTrueName);
                        Thread.Sleep(500);
                        webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=say [BattleCupBot] Правильный тег команды Б: " + ctTrueName);
                    }
                }
                else
                {
                    WorkClass.PrintLogMessage("INCORRECT TAGS " + tRegex + " - " + tTrueName + "; " + ctRegex + " - " + ctTrueName, "DEBUG");
                    webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=say [BattleCupBot] Пожалуйста, поставьте правильные теги команд, иначе результат матча не будет учтен");
                    Thread.Sleep(500);
                    webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=say [BattleCupBot] Правильный тег команды А: " + tTrueName);
                    Thread.Sleep(500);
                    webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=say [BattleCupBot] Правильный тег команды Б: " + ctTrueName);
                }

            EmptyTag:
                if (PlayedMatchesHook(ctTrueName, tTrueName, dateFrom, dateTo, tPart))
                {
                    return;
                }
                Thread.Sleep(30000);
            }
        }

        private static bool PlayedMatchesHook(string ct, string t, string dateFrom, string dateTo, string tPart)
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT TeamA, TeamAScore, TeamB, TeamBScore, ID FROM matches WHERE MatchDate > '" + DateTime.Parse(dateFrom).ToString("yyyy-MM-dd HH:mm:ss") + "' AND MatchDate < '" + DateTime.Parse(dateTo).ToString("yyyy-MM-dd HH:mm:ss") + "';", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                // WARNING NOT CHECKED
                if ((reader[0].ToString() == t && reader[2].ToString() == ct) || (reader[0].ToString() == ct && reader[2].ToString() == t))
                {
                    var matchId = reader[4].ToString();
                    var tScore = int.Parse(reader[1].ToString());
                    var ctScore = int.Parse(reader[3].ToString());
                    var tName = reader[0].ToString();
                    var ctName = reader[2].ToString();
                    if (tScore > ctScore)
                    {
                        reader.Close();
                        var insertQuery = new MySqlCommand("UPDATE battlecup_matches SET MatchId = '" + matchId + "', MatchPlayed = '1', MatchWinnerName = '" + tName + "' WHERE ((TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "') OR (TeamAName = '" + ctName + "' OR TeamAName = '" + tName + "')) AND TournamentId = '" + BCupId + "' AND BracketItem = '" + tPart + "';", connection);
                        insertQuery.ExecuteNonQuery();
                        connection.Close();
                        return true;
                    }
                    else
                    {
                        reader.Close();
                        var insertQuery = new MySqlCommand("UPDATE battlecup_matches SET MatchId = '" + matchId + "', MatchPlayed = '1', MatchWinnerName = '" + ctName + "' WHERE ((TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "') OR (TeamAName = '" + ctName + "' OR TeamAName = '" + tName + "')) AND TournamentId = '" + BCupId + "' AND BracketItem = '" + tPart + "';", connection);
                        insertQuery.ExecuteNonQuery();
                        connection.Close();
                        return true;
                    }
                }
            }
            connection.Close();
            return false;
        }
    }
}
