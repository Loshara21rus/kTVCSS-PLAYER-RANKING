using MySqlConnector;
using System;
using System.Collections.Generic;
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
    public static class Cups
    {
        #region global vars

        private static int _status = 0;
        public static int CupId = 0;
        public static string CupName = "";
        public static WebClient Web = new WebClient();
        public static long BoardId = 0;
        public static Random Random = new Random();
        public static List<string> Teams = new List<string>();
        public static long PostId = 0;
        public static int Password = 0;

        #endregion

        public static void Init()
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand($"SELECT Status FROM cups_list WHERE ISFINISHED = 0;", connection);
            var reader = query.ExecuteReader();
            var isAlreadyCup = reader.HasRows;

            while (reader.Read())
            {
                Status = int.Parse(reader[0].ToString());
            }

            connection.Close();

            if (isAlreadyCup)
            {
                if (Status == 0)
                {
                    BeginProcess(0);
                }
                // Get Data From Recovery
                PostId = long.Parse(GetDataFromRecovery("postId"));
                CupId = int.Parse(GetDataFromRecovery("CupId"));
                CupName = GetDataFromRecovery("CupName");
                BoardId = long.Parse(GetDataFromRecovery("RegBoardId"));
                PostId = long.Parse(GetDataFromRecovery("postId"));

                var tempTeamsRow = GetDataFromRecovery("Teams");
                if (tempTeamsRow != "0")
                {
                    Teams.AddRange(tempTeamsRow.Split(';'));
                }

                Password = int.Parse(GetDataFromRecovery("password"));

                if (Status >= 1 && Status <= 3)
                {
                    BeginProcess(Status);
                }
                if (Status >= 4 && Status <= 5)
                {
                    RegProcess(Status);
                }
                if (Status == 6)
                {
                    ShuffleTeams(Status);
                }
                if (Status >= 7 && Status <= 10)
                {
                    MakeSchedule(Status);
                }
                if (Status >= 11 && Status <= 34)
                {
                    MainProcess(Status);
                }
            }
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
                WorkClass.PrintLogMessage("Cup status changed to " + value, "TRACE");
            }
        }

        private static string GetDataFromRecovery(string paramName)
        {
            string toReturn = null;
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT " + paramName + " FROM cups_recovery;", connection);
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
            var query = new MySqlCommand("UPDATE cups_recovery SET " + paramName + " = '" + value + "' WHERE " + paramName + " = '0';", connection);
            query.ExecuteNonQuery();
            connection.Close();
        }

        private static void UpdateCupStatus(string Name, int statusId)
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("UPDATE cups_list SET Status = '" + statusId + "' WHERE Name = '" + Name + "';", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                CupId = int.Parse(reader[0].ToString());
            }
            connection.Close();
            Status = statusId;
        }

        private static void BeginProcess(int recoverId)
        {
            var api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = WorkClass.Token,
            });

            if (recoverId != 0)
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
            var query = new MySqlCommand($"SELECT ID, NAME FROM cups_list WHERE ISFINISHED = 0;", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                CupId = int.Parse(reader[0].ToString());
                CupName = reader[1].ToString();
            }
            reader.Close();
            connection.Close();

            Bracket.UpdateDbBracket(0, CupName);

            InsertDataToRecovery("CupId", CupId.ToString());
            InsertDataToRecovery("CupName", CupName);
            UpdateCupStatus(CupName, 1);

        CupStatusId1:
            BoardId = api.Board.AddTopic(new BoardAddTopicParams
            {
                GroupId = WorkClass.MainGroupId,
                FromGroup = true,
                Text = "Формат заявки (иначе будет автоматически удаляться):\r\n1. Название команды " +
                	"(которое будет в игре)\r\n2. Состав команды (включая STEAM ID)\r\n\r\n" +
                	"Если заявки будут не полными, например, будет тупой комментарий ''забил'' или ''Кентыхдксс позже заполню'' " +
                    "или не будет хватать пяти стим айдишников, заявка будет автоматически удаляться.\r\nПравила турниров: https://v34.ktvcss.org.ru/rules.php",
                Title = CupName
            });
            InsertDataToRecovery("RegBoardId", BoardId.ToString());
            UpdateCupStatus(CupName, 2);

        CupStatusId2:
            var desc = "";
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT DESCRIPTION FROM cups_list WHERE Id = " + CupId + ";", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                desc = reader[0].ToString();
            }
            connection.Close();
            api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = "Открыта регистрация на турнир " + CupName + "\r\n" + desc + "\r\nРегистрация: https://vk.com/topic-" + WorkClass.MainGroupId + "_" + BoardId,
                FromGroup = true,
                Signed = false,
            });
            UpdateCupStatus(CupName, 3);
        CupStatusId3:
            RegProcess(Status);
        }

        private static void RegProcess(int recoverId)
        {
            var count = 0;
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT TEAMSCOUNT FROM cups_list WHERE Id = " + CupId + ";", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                count = int.Parse(reader[0].ToString());
            }
            connection.Close();
            var api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = WorkClass.Token,
            });

            var messages = api.Board.GetComments(new BoardGetCommentsParams()
            {
                GroupId = WorkClass.MainGroupId,
                TopicId = BoardId,
                Count = count
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
            while (messages.Count < count + 1)
            {
                messages = api.Board.GetComments(new BoardGetCommentsParams()
                {
                    GroupId = WorkClass.MainGroupId,
                    TopicId = BoardId,
                    Count = count + 1
                });

                foreach (var entry in messages.Items)
                {
                    if (entry.FromId == -WorkClass.MainGroupId) continue;
                    if (!VerifyEntry(entry.Text, entry.FromId))
                    {
                        api.Board.DeleteComment(new BoardCommentParams()
                        {
                            CommentId = entry.Id,
                            GroupId = WorkClass.MainGroupId,
                            TopicId = BoardId
                        });
                    }
                }
                Thread.Sleep(60 * 100);
            }

            UpdateCupStatus(CupName, 4);

            CupStatusId4:

            var entryToDB = new Dictionary<string, List<string>>();

            messages = api.Board.GetComments(new BoardGetCommentsParams()
            {
                GroupId = WorkClass.MainGroupId,
                TopicId = BoardId,
                Count = count + 1
            });
            if (messages.Count != count + 1)
            {
                UpdateCupStatus(CupName, 3);
                Environment.Exit(0);
            }
            else
            {
                foreach (var entry in messages.Items)
                {
                    if (entry.FromId == -WorkClass.MainGroupId) continue;
                    if (!VerifyEntry(entry.Text, entry.FromId))
                    {
                        api.Board.DeleteComment(new BoardCommentParams()
                        {
                            CommentId = entry.Id,
                            GroupId = WorkClass.MainGroupId,
                            TopicId = BoardId
                        });
                        UpdateCupStatus(CupName, 3);
                        Environment.Exit(0);
                    }
                    else
                    {
                        var teamName = entry.Text.Split('\n')[0].Substring(2).Trim();
                        var steamIds = Regex.Matches(entry.Text, @"STEAM_[0-5]:[01]:\d+", RegexOptions.Multiline);
                        var entrySteams = new List<string>();
                        foreach (var steam in steamIds)
                        {
                            entrySteams.Add(steam.ToString());
                        }
                        if (entryToDB.Count == 0)
                        {
                            entryToDB.Add(teamName, entrySteams);
                            continue;
                        }
                        for (int i = 0; i < entryToDB.Count(); i++)
                        {
                            foreach (var steam in entrySteams)
                            {
                                if (entryToDB.Values.ElementAt(i).Contains(steam))
                                {
                                    SendErrorToUser(entry.FromId, "Указан стим айди, который уже имеется в какой-либо заявке (" + steam + ")");
                                    api.Board.DeleteComment(new BoardCommentParams()
                                    {
                                        CommentId = entry.Id,
                                        GroupId = WorkClass.MainGroupId,
                                        TopicId = BoardId
                                    });
                                    UpdateCupStatus(CupName, 3);
                                    Environment.Exit(0);
                                }
                            }
                        }
                        entryToDB.Add(teamName, entrySteams);
                    }
                }
                api.Board.CloseTopic(new BoardTopicParams()
                {
                    GroupId = WorkClass.MainGroupId,
                    TopicId = BoardId
                });
                foreach (var team in entryToDB)
                {
                    var stuff = "";
                    foreach (var item in team.Value)
                    {
                        stuff += item + ";";
                    }
                    stuff = stuff.Remove(stuff.Length - 1, 1);
                    connection = new MySqlConnection(WorkClass.ConnectionString);
                    connection.Open();
                    query = new MySqlCommand($"INSERT INTO cups_teams (`CID`, `CNAME`, `TNAME`, `TSTUFF`) VALUES ('{CupId}', '{CupName}', '{team.Key}', '{stuff}');", connection);
                    query.ExecuteNonQuery();
                    connection.Close();
                }

                UpdateCupStatus(CupName, 5);
            }

        CupStatusId5:
            ShuffleTeams(Status);
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
            var inTeams = new List<string>();
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT TNAME FROM cups_teams WHERE CID = " + CupId + ";", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                inTeams.Add(reader[0].ToString());
            }
            connection.Close();
            foreach (var s in inTeams)
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
            UpdateCupStatus(CupName, 6);
        CupStatusId6:
            MakeSchedule(Status);
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

            UpdateCupStatus(CupName, 7);
        CupStatusId7:
            DateTime sDate = new DateTime();
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT STARTDATE FROM cups_list WHERE ID = " + CupId + ";", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                sDate = DateTime.Parse(reader[0].ToString());
            }
            connection.Close();
            // first items in bracket
            for (int i = 0; i < 16; i++)
            {
                Bracket.UpdateDbBracket(i+1, Teams[i]);
            }
            Bracket.Draw();
            // linux path
            var uploadServer = api.Photo.GetWallUploadServer(WorkClass.MainGroupId);
            var result = Encoding.ASCII.GetString(Web.UploadFile(uploadServer.UploadUrl, @"Images\Cups\bracket_work.png"));
            var photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.MainGroupId);

            PostId = api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = "Расписание на турнир " + CupName + " @ " + sDate.ToString("dd.MM.yyyy") + " 20:00 bo3\r\n"
                 + Teams[0] + " vs " + Teams[1] + " @ kTVCSS №1\r\n"
                 + Teams[2] + " vs " + Teams[3] + " @ kTVCSS №2\r\n"
                  + Teams[4] + " vs " + Teams[5] + " @ kTVCSS №3\r\n"
                  + Teams[6] + " vs " + Teams[7] + " @ kTVCSS №4\r\n"
                  + Teams[8] + " vs " + Teams[9] + " @ kTVCSS №5\r\n"
                  + Teams[10] + " vs " + Teams[11] + " @ kTVCSS №6\r\n"
                  + Teams[12] + " vs " + Teams[13] + " @ kTVCSS №7\r\n"
                  + Teams[14] + " vs " + Teams[15] + " @ kTVCSS №8\r\n" +
                      "Правила турниров: https://v34.ktvcss.org.ru/rules.php\r\n" +
                      "По любым вопросам, относящимся к турниру, обращаться к @waneda_avganskaya(Waneda) или @jekacheater(SNAX).",
                FromGroup = true,
                Signed = false,
                Attachments = new List<MediaAttachment>
                {
                    photo.FirstOrDefault()
                }
            });

            InsertDataToRecovery("postId", PostId.ToString());
            UpdateCupStatus(CupName, 8);

        CupStatusId8:
            Password = Random.Next(1000, 2000);
            InsertDataToRecovery("password", Password.ToString());

            var count = 0;
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT TEAMSCOUNT FROM cups_list WHERE Id = " + CupId + ";", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                count = int.Parse(reader[0].ToString());
            }
            connection.Close();

            var messages = api.Board.GetComments(new BoardGetCommentsParams()
            {
                GroupId = WorkClass.MainGroupId,
                TopicId = BoardId,
                Count = count + 1
            });
            for (int i = 1; i < messages.Items.Count; i++)
            {
                try
                {
                    api.Messages.Send(new MessagesSendParams()
                    {
                        GroupId = (ulong)WorkClass.MainGroupId,
                        PeerId = messages.Items[i].FromId,
                        Message = "Здравствуйте! Вы участвуете в турнире " + CupName + "!" +
                        	"\r\nСсылка на расписание матчей: https://vk.com/ktvcss?w=wall-" + WorkClass.MainGroupId + "_" + PostId + 
                        "\r\nПароль от серверов на весь турнир: " + Password.ToString() + 
                            "\r\nВам следует знать несколько вещей перед тем, как играть матчи:\r\n" +
                            "1. На сервере Вашей необходимо ставить тег, который Вы указали при регистрации, иначе результаты матчей не будут учитываться. В любом случае, на сервере будет об этом напоминание.\r\n" +
                            "2. В день матча в 18:55 по мск будут автоматически освобождаться сервера (будут кикаться все игроки с сервера и будет установлен пароль). На Боевых Кубках это делается в 17:55, т.к. турнир всегда начинается в 18:00. Здесь же время начала матча может быть и 19:00, и 20:00, и позже. Поэтому дабы избежать неприятных ситуаций было выбрано указанное время.\r\n" +
                            "Желаем Вам удачи на турнире!\r\nРассылка выполнена с помощью kTVCSSCupBot.",
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
                            Message = "Здравствуйте! Вы участвуете в турнире " + CupName + "!" +
                            "\r\nСсылка на расписание матчей: https://vk.com/ktvcss?w=wall-" + WorkClass.MainGroupId + "_" + PostId +
                        "\r\nПароль от серверов на весь турнир: " + Password.ToString() +
                            "\r\nВам следует знать несколько вещей перед тем, как играть матчи:\r\n" +
                            "1. На сервере Вашей необходимо ставить тег, который Вы указали при регистрации, иначе результаты матчей не будут учитываться. В любом случае, на сервере будет об этом напоминание.\r\n" +
                            "2. В день матча в 18:55 по мск будут автоматически освобождаться сервера (будут кикаться все игроки с сервера и будет установлен пароль). На Боевых Кубках это делается в 17:55, т.к. турнир всегда начинается в 18:00. Здесь же время начала матча может быть и 19:00, и 20:00, и позже. Поэтому дабы избежать неприятных ситуаций было выбрано указанное время.\r\n" +
                            "Желаем Вам удачи на турнире!\r\nРассылка выполнена с помощью kTVCSSCupBot.",
                            RandomId = new Random().Next()
                        });
                    }
                    catch (Exception)
                    {
                        // Ignored
                    }
                }
                Thread.Sleep(1000);
            }
            UpdateCupStatus(CupName, 9);
        CupStatusId9:
            sDate = new DateTime();
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT STARTDATE FROM cups_list WHERE ID = " + CupId + ";", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                sDate = DateTime.Parse(reader[0].ToString());
            }
            connection.Close();

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
            	" VALUES ('" + Teams[0] + "', '" + Teams[1] + "', '" + CupId + "', '0', '" + sDate.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + sDate.ToString("yyyy-MM-dd") + " 23:59:59', '1/8ub', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
            	" VALUES ('" + Teams[2] + "', '" + Teams[3] + "', '" + CupId + "', '1', '" + sDate.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + sDate.ToString("yyyy-MM-dd") + " 23:59:59', '1/8ub', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
            	" VALUES ('" + Teams[4] + "', '" + Teams[5] + "', '" + CupId + "', '2', '" + sDate.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + sDate.ToString("yyyy-MM-dd") + " 23:59:59', '1/8ub', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
            	" VALUES ('" + Teams[6] + "', '" + Teams[7] + "', '" + CupId + "', '3', '" + sDate.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + sDate.ToString("yyyy-MM-dd") + " 23:59:59', '1/8ub', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + Teams[8] + "', '" + Teams[9] + "', '" + CupId + "', '4', '" + sDate.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + sDate.ToString("yyyy-MM-dd") + " 23:59:59', '1/8ub', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + Teams[10] + "', '" + Teams[11] + "', '" + CupId + "', '5', '" + sDate.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + sDate.ToString("yyyy-MM-dd") + " 23:59:59', '1/8ub', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + Teams[12] + "', '" + Teams[13] + "', '" + CupId + "', '6', '" + sDate.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + sDate.ToString("yyyy-MM-dd") + " 23:59:59', '1/8ub', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + Teams[14] + "', '" + Teams[15] + "', '" + CupId + "', '7', '" + sDate.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + sDate.ToString("yyyy-MM-dd") + " 23:59:59', '1/8ub', 'bo3');", connection);
            query.ExecuteNonQuery();

            connection.Close();

            UpdateCupStatus(CupName, 10);
            CupStatusId10:

            MainProcess(10);
        }

        private static DateTime GetPartDay(string part)
        {
            string date = "";
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand($"SELECT DateTimeStart FROM cups_matches WHERE BracketItem = '{part}' AND TournamentId = '" + CupId + "';", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                date = reader[0].ToString();
            }
            connection.Close();
            return DateTime.Parse(date);
        }

        private static void MainProcess(int recoverId)
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
                    case 20:
                        {
                            goto CupStatusId20;
                        }
                    case 21:
                        {
                            goto CupStatusId21;
                        }
                    case 22:
                        {
                            goto CupStatusId22;
                        }
                    case 23:
                        {
                            goto CupStatusId23;
                        }
                    case 24:
                        {
                            goto CupStatusId24;
                        }
                    case 25:
                        {
                            goto CupStatusId25;
                        }
                    case 26:
                        {
                            goto CupStatusId26;
                        }
                    case 27:
                        {
                            goto CupStatusId27;
                        }
                    case 28:
                        {
                            goto CupStatusId28;
                        }
                    case 29:
                        {
                            goto CupStatusId29;
                        }
                    case 30:
                        {
                            goto CupStatusId30;
                        }
                    case 31:
                        {
                            goto CupStatusId31;
                        }
                    case 32:
                        {
                            goto CupStatusId32;
                        }
                    case 33:
                        {
                            goto CupStatusId33;
                        }
                    case 34:
                        {
                            goto CupStatusId34;
                        }
                }
            }

            // 1/8 upper bracket
            while (!IsTimeToKick(GetPartDay("1/8ub")))
            {
                Thread.Sleep(30000);
            }

            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT ServerId FROM cups_matches WHERE BracketItem = '1/8ub' AND TournamentId = '" + CupId + "';", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                KickFromServer(WorkClass.ServerList[int.Parse(reader[0].ToString())].Host, WorkClass.ServerList[int.Parse(reader[0].ToString())].GamePort.ToString(), WorkClass.ServerList[int.Parse(reader[0].ToString())].RconPassword);
            }
            connection.Close();

            UpdateCupStatus(CupName, 11);
        CupStatusId11:
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId, TeamAName, TeamBName, DateTimeStart, DateTimeEnd FROM cups_matches WHERE BracketItem = '1/8ub' AND TournamentId = '" + CupId + "' AND MatchPlayed = 0;", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                var check = new Thread(CheckTrueTeamNames);
                check.Start(reader[0].ToString() + ";" + reader[1].ToString() + ";" + reader[2].ToString() + ";" + reader[3].ToString() + ";" + reader[4].ToString() + ";1/8ub");
            }
            connection.Close();

            while (!CheckIsPartFinished(8, "1/8ub"))
            {
                Thread.Sleep(15000);
            }

            UpdateCupStatus(CupName, 12);
            CupStatusId12:

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();

            var winners = new List<string>();
            var loosers = new List<string>();
            var datetime = new DateTime();

            // upper bracket 1/4
            query = new MySqlCommand("SELECT MatchWinnerName, DateTimeStart FROM cups_matches WHERE BracketItem = '1/8ub' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                winners.Add(reader[0].ToString());
                datetime = DateTime.Parse(reader[1].ToString());
            }
            reader.Close();

            datetime = datetime.AddDays(1);

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + winners[0] + "', '" + winners[1] + "', '" + CupId + "', '0', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '1/4ub', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + winners[2] + "', '" + winners[3] + "', '" + CupId + "', '1', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '1/4ub', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + winners[4] + "', '" + winners[5] + "', '" + CupId + "', '2', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '1/4ub', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + winners[6] + "', '" + winners[7] + "', '" + CupId + "', '3', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '1/4ub', 'bo3');", connection);
            query.ExecuteNonQuery();

            // lower bracket 1 round
            query = new MySqlCommand("SELECT MatchLooserName FROM cups_matches WHERE BracketItem = '1/8ub' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                loosers.Add(reader[0].ToString());
            }
            reader.Close();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + loosers[0] + "', '" + loosers[1] + "', '" + CupId + "', '4', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '1rl', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + loosers[2] + "', '" + loosers[3] + "', '" + CupId + "', '5', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '1rl', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + loosers[4] + "', '" + loosers[5] + "', '" + CupId + "', '6', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '1rl', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + loosers[6] + "', '" + loosers[7] + "', '" + CupId + "', '7', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '1rl', 'bo3');", connection);
            query.ExecuteNonQuery();

            connection.Close();

            int k = 0;

            for (int i = 16; i < 24; i++)
            {
                Bracket.UpdateDbBracket(i + 1, winners[k]); // 1/4 ub
                k++;
            }

            k = 0;

            for (int i = 24; i < 32; i++)
            {
                Bracket.UpdateDbBracket(i + 1, loosers[k]); // 1rl
                k++;
            }

            k = 0;

            Bracket.Draw();

            // linux path
            var uploadServer = api.Photo.GetWallUploadServer(WorkClass.MainGroupId);
            var result = Encoding.ASCII.GetString(Web.UploadFile(uploadServer.UploadUrl, @"Images\Cups\bracket_work.png"));
            var photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.MainGroupId);

            PostId = api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = "Расписание на турнир " + CupName + " @ " + datetime.ToString("dd.MM.yyyy") + " 20:00 bo3\r\n"
                 + winners[0] + " vs " + winners[1] + " @ kTVCSS №1\r\n"
                 + winners[2] + " vs " + winners[3] + " @ kTVCSS №2\r\n"
                  + winners[4] + " vs " + winners[5] + " @ kTVCSS №3\r\n"
                  + winners[6] + " vs " + winners[7] + " @ kTVCSS №4\r\n"
                  + loosers[0] + " vs " + loosers[1] + " @ kTVCSS №5\r\n"
                  + loosers[2] + " vs " + loosers[3] + " @ kTVCSS №6\r\n"
                  + loosers[4] + " vs " + loosers[5] + " @ kTVCSS №7\r\n"
                  + loosers[6] + " vs " + loosers[7] + " @ kTVCSS №8\r\n" +
                      "Правила турниров: https://v34.ktvcss.org.ru/rules.php\r\n" +
                      "По любым вопросам, относящимся к турниру, обращаться к @waneda_avganskaya(Waneda) или @jekacheater(SNAX).",
                FromGroup = true,
                Signed = false,
                Attachments = new List<MediaAttachment>
                {
                   photo.FirstOrDefault()
                }
            });

            InsertDataToRecovery("postId", PostId.ToString());

            UpdateCupStatus(CupName, 13);

            CupStatusId13:

            // 1/4 upper bracket and 1rl
            while (!IsTimeToKick(GetPartDay("1/4ub")))
            {
                Thread.Sleep(30000);
            }

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId FROM cups_matches WHERE (BracketItem = '1/4ub' OR BracketItem = '1rl') AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                KickFromServer(WorkClass.ServerList[int.Parse(reader[0].ToString())].Host, WorkClass.ServerList[int.Parse(reader[0].ToString())].GamePort.ToString(), WorkClass.ServerList[int.Parse(reader[0].ToString())].RconPassword);
            }
            connection.Close();

            UpdateCupStatus(CupName, 14);

            CupStatusId14:

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId, TeamAName, TeamBName, DateTimeStart, DateTimeEnd FROM cups_matches WHERE BracketItem = '1/4ub' AND TournamentId = '" + CupId + "' AND MatchPlayed = 0;", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                var check = new Thread(CheckTrueTeamNames);
                check.Start(reader[0].ToString() + ";" + reader[1].ToString() + ";" + reader[2].ToString() + ";" + reader[3].ToString() + ";" + reader[4].ToString() + ";1/4ub");
            }
            connection.Close();

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId, TeamAName, TeamBName, DateTimeStart, DateTimeEnd FROM cups_matches WHERE BracketItem = '1rl' AND TournamentId = '" + CupId + "' AND MatchPlayed = 0;", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                var check = new Thread(CheckTrueTeamNames);
                check.Start(reader[0].ToString() + ";" + reader[1].ToString() + ";" + reader[2].ToString() + ";" + reader[3].ToString() + ";" + reader[4].ToString() + ";1rl");
            }
            connection.Close();

            while (!CheckIsPartFinished(4, "1/4ub"))
            {
                Thread.Sleep(15000);
            }

            while (!CheckIsPartFinished(4, "1rl"))
            {
                Thread.Sleep(15000);
            }

            UpdateCupStatus(CupName, 15);
            CupStatusId15:

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();

            winners = new List<string>();
            loosers = new List<string>();
            datetime = new DateTime();

            // upper bracket 1/2
            query = new MySqlCommand("SELECT MatchWinnerName, MatchLooserName, DateTimeStart FROM cups_matches WHERE BracketItem = '1/4ub' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                winners.Add(reader[0].ToString());
                datetime = DateTime.Parse(reader[2].ToString());
                loosers.Add(reader[1].ToString());
            }
            reader.Close();

            datetime = datetime.AddDays(1);

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + winners[0] + "', '" + winners[1] + "', '" + CupId + "', '0', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '1/2ub', 'bo3');", connection);
            query.ExecuteNonQuery();
            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + winners[2] + "', '" + winners[3] + "', '" + CupId + "', '1', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '1/2ub', 'bo3');", connection);
            query.ExecuteNonQuery();

            // lower bracket 2 round
            var lWinners = new List<string>();
            query = new MySqlCommand("SELECT MatchWinnerName FROM cups_matches WHERE BracketItem = '1rl' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                lWinners.Add(reader[0].ToString());
            }
            reader.Close();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + loosers[0] + "', '" + lWinners[0] + "', '" + CupId + "', '2', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '2rl', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
               " VALUES ('" + loosers[1] + "', '" + lWinners[1] + "', '" + CupId + "', '3', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '2rl', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
               " VALUES ('" + loosers[2] + "', '" + lWinners[2] + "', '" + CupId + "', '4', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '2rl', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
               " VALUES ('" + loosers[3] + "', '" + lWinners[3] + "', '" + CupId + "', '5', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '2rl', 'bo3');", connection);
            query.ExecuteNonQuery();

            connection.Close();

            k = 0;

            for (int i = 32; i < 36; i++)
            {
                Bracket.UpdateDbBracket(i + 1, winners[k]); // 1/2 ub
                k++;
            }

            k = 0;

            for (int i = 36; i < 44; i++)
            {
                Bracket.UpdateDbBracket(i + 1, loosers[k]); // 2rl
                i++;
                Bracket.UpdateDbBracket(i + 1, lWinners[k]); // 2rl
                k++;
            }

            k = 0;

            Bracket.Draw();

            // linux path
            uploadServer = api.Photo.GetWallUploadServer(WorkClass.MainGroupId);
            result = Encoding.ASCII.GetString(Web.UploadFile(uploadServer.UploadUrl, @"Images\Cups\bracket_work.png"));
            photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.MainGroupId);

            PostId = api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = "Расписание на турнир " + CupName + " @ " + datetime.ToString("dd.MM.yyyy") + " 20:00 bo3\r\n"
                 + winners[0] + " vs " + winners[1] + " @ kTVCSS №1\r\n"
                 + winners[2] + " vs " + winners[3] + " @ kTVCSS №2\r\n"
                  + loosers[0] + " vs " + lWinners[0] + " @ kTVCSS №3\r\n"
                  + loosers[1] + " vs " + lWinners[1] + " @ kTVCSS №4\r\n"
                  + loosers[2] + " vs " + lWinners[2] + " @ kTVCSS №5\r\n"
                  + loosers[3] + " vs " + lWinners[3] + " @ kTVCSS №6\r\n"
                  + "Правила турниров: https://v34.ktvcss.org.ru/rules.php\r\n" +
                      "По любым вопросам, относящимся к турниру, обращаться к @waneda_avganskaya(Waneda) или @jekacheater(SNAX).",
                FromGroup = true,
                Signed = false,
                Attachments = new List<MediaAttachment>
                {
                   photo.FirstOrDefault()
                }
            });

            InsertDataToRecovery("postId", PostId.ToString());

            UpdateCupStatus(CupName, 16);

        CupStatusId16:

            // 1/2 upper bracket and 2rl
            while (!IsTimeToKick(GetPartDay("1/2ub")))
            {
                Thread.Sleep(30000);
            }

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId FROM cups_matches WHERE (BracketItem = '1/2ub' OR BracketItem = '2rl') AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                KickFromServer(WorkClass.ServerList[int.Parse(reader[0].ToString())].Host, WorkClass.ServerList[int.Parse(reader[0].ToString())].GamePort.ToString(), WorkClass.ServerList[int.Parse(reader[0].ToString())].RconPassword);
            }
            connection.Close();

            UpdateCupStatus(CupName, 17);

        CupStatusId17:

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId, TeamAName, TeamBName, DateTimeStart, DateTimeEnd FROM cups_matches WHERE BracketItem = '1/2ub' AND TournamentId = '" + CupId + "' AND MatchPlayed = 0;", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                var check = new Thread(CheckTrueTeamNames);
                check.Start(reader[0].ToString() + ";" + reader[1].ToString() + ";" + reader[2].ToString() + ";" + reader[3].ToString() + ";" + reader[4].ToString() + ";1/2ub");
            }
            connection.Close();

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId, TeamAName, TeamBName, DateTimeStart, DateTimeEnd FROM cups_matches WHERE BracketItem = '2rl' AND TournamentId = '" + CupId + "' AND MatchPlayed = 0;", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                var check = new Thread(CheckTrueTeamNames);
                check.Start(reader[0].ToString() + ";" + reader[1].ToString() + ";" + reader[2].ToString() + ";" + reader[3].ToString() + ";" + reader[4].ToString() + ";2rl");
            }
            connection.Close();

            while (!CheckIsPartFinished(2, "1/2ub"))
            {
                Thread.Sleep(15000);
            }

            while (!CheckIsPartFinished(4, "2rl"))
            {
                Thread.Sleep(15000);
            }

            UpdateCupStatus(CupName, 18);

        CupStatusId18:

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();

            winners = new List<string>();
            loosers = new List<string>();
            datetime = new DateTime();

            // upper bracket final
            query = new MySqlCommand("SELECT MatchWinnerName, MatchLooserName, DateTimeStart FROM cups_matches WHERE BracketItem = '1/2ub' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                winners.Add(reader[0].ToString());
                datetime = DateTime.Parse(reader[2].ToString());
                loosers.Add(reader[1].ToString());
            }
            reader.Close();

            datetime = datetime.AddDays(1);

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + winners[0] + "', '" + winners[1] + "', '" + CupId + "', '0', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', 'ubf', 'bo3');", connection);
            query.ExecuteNonQuery();

            // lower bracket 3 round
            lWinners = new List<string>();
            query = new MySqlCommand("SELECT MatchWinnerName FROM cups_matches WHERE BracketItem = '2rl' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                lWinners.Add(reader[0].ToString());
            }
            reader.Close();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + lWinners[0] + "', '" + lWinners[1] + "', '" + CupId + "', '1', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '3rl', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + lWinners[2] + "', '" + lWinners[3] + "', '" + CupId + "', '2', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '3rl', 'bo3');", connection);
            query.ExecuteNonQuery();

            connection.Close();

            k = 0;

            for (int i = 44; i < 46; i++)
            {
                Bracket.UpdateDbBracket(i + 1, winners[k]); // ubf
                k++;
            }

            k = 0;

            for (int i = 46; i < 50; i++)
            {
                Bracket.UpdateDbBracket(i + 1, lWinners[k]); // 3rl
                k++;
            }

            k = 0;

            Bracket.Draw();

            // linux path
            uploadServer = api.Photo.GetWallUploadServer(WorkClass.MainGroupId);
            result = Encoding.ASCII.GetString(Web.UploadFile(uploadServer.UploadUrl, @"Images\Cups\bracket_work.png"));
            photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.MainGroupId);

            PostId = api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = "Расписание на турнир " + CupName + " @ " + datetime.ToString("dd.MM.yyyy") + " 20:00 bo3\r\n"
                 + winners[0] + " vs " + winners[1] + " @ kTVCSS №1\r\n"
                  + lWinners[0] + " vs " + lWinners[1] + " @ kTVCSS №2\r\n"
                  + lWinners[2] + " vs " + lWinners[3] + " @ kTVCSS №3\r\n"
                  + "Правила турниров: https://v34.ktvcss.org.ru/rules.php\r\n" +
                      "По любым вопросам, относящимся к турниру, обращаться к @waneda_avganskaya(Waneda) или @jekacheater(SNAX).",
                FromGroup = true,
                Signed = false,
                Attachments = new List<MediaAttachment>
                {
                   photo.FirstOrDefault()
                }
            });

            InsertDataToRecovery("postId", PostId.ToString());

            UpdateCupStatus(CupName, 19);

        CupStatusId19:

            // upper bracket final and 3rl
            while (!IsTimeToKick(GetPartDay("ubf")))
            {
                Thread.Sleep(30000);
            }

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId FROM cups_matches WHERE (BracketItem = 'ubf' OR BracketItem = '3rl') AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                KickFromServer(WorkClass.ServerList[int.Parse(reader[0].ToString())].Host, WorkClass.ServerList[int.Parse(reader[0].ToString())].GamePort.ToString(), WorkClass.ServerList[int.Parse(reader[0].ToString())].RconPassword);
            }
            connection.Close();

            UpdateCupStatus(CupName, 20);

        CupStatusId20:

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId, TeamAName, TeamBName, DateTimeStart, DateTimeEnd FROM cups_matches WHERE BracketItem = 'ubf' AND TournamentId = '" + CupId + "' AND MatchPlayed = 0;", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                var check = new Thread(CheckTrueTeamNames);
                check.Start(reader[0].ToString() + ";" + reader[1].ToString() + ";" + reader[2].ToString() + ";" + reader[3].ToString() + ";" + reader[4].ToString() + ";ubf");
            }
            connection.Close();

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId, TeamAName, TeamBName, DateTimeStart, DateTimeEnd FROM cups_matches WHERE BracketItem = '3rl' AND TournamentId = '" + CupId + "' AND MatchPlayed = 0;", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                var check = new Thread(CheckTrueTeamNames);
                check.Start(reader[0].ToString() + ";" + reader[1].ToString() + ";" + reader[2].ToString() + ";" + reader[3].ToString() + ";" + reader[4].ToString() + ";3rl");
            }
            connection.Close();

            while (!CheckIsPartFinished(1, "ubf"))
            {
                Thread.Sleep(15000);
            }

            while (!CheckIsPartFinished(2, "3rl"))
            {
                Thread.Sleep(15000);
            }

            UpdateCupStatus(CupName, 21);

        CupStatusId21:
        
            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();

            winners = new List<string>();
            loosers = new List<string>();
            datetime = new DateTime();

            // get from ubf
            var ubfw = "";
            var ubfl = "";
            query = new MySqlCommand("SELECT MatchWinnerName, MatchLooserName FROM cups_matches WHERE BracketItem = 'ubf' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                ubfw = reader[0].ToString();
                ubfl = reader[1].ToString();
            }
            reader.Close();

            // get from 1/2 for 4rl
            query = new MySqlCommand("SELECT MatchWinnerName, MatchLooserName, DateTimeStart FROM cups_matches WHERE BracketItem = '1/2ub' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                winners.Add(reader[0].ToString());
                datetime = DateTime.Parse(reader[2].ToString());
                loosers.Add(reader[1].ToString());
            }
            reader.Close();

            datetime = datetime.AddDays(1);

            // lower bracket 3 round results
            lWinners = new List<string>();
            query = new MySqlCommand("SELECT MatchWinnerName, DateTimeStart FROM cups_matches WHERE BracketItem = '3rl' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                lWinners.Add(reader[0].ToString());
                datetime = DateTime.Parse(reader[1].ToString());
            }
            reader.Close();

            datetime = datetime.AddDays(1);

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + loosers[0] + "', '" + lWinners[0] + "', '" + CupId + "', '0', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '4rl', 'bo3');", connection);
            query.ExecuteNonQuery();

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + loosers[1] + "', '" + lWinners[1] + "', '" + CupId + "', '1', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', '4rl', 'bo3');", connection);
            query.ExecuteNonQuery();

            connection.Close();

            k = 0;

            Bracket.UpdateDbBracket(51, ubfw);
            Bracket.UpdateDbBracket(52, ubfl);

            for (int i = 52; i < 56; i++)
            {
                Bracket.UpdateDbBracket(i + 1, loosers[k]); // 4rl
                i++;
                Bracket.UpdateDbBracket(i + 1, lWinners[k]); // 4rl
                k++;
            }

            k = 0;

            Bracket.Draw();

            // linux path
            uploadServer = api.Photo.GetWallUploadServer(WorkClass.MainGroupId);
            result = Encoding.ASCII.GetString(Web.UploadFile(uploadServer.UploadUrl, @"Images\Cups\bracket_work.png"));
            photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.MainGroupId);

            PostId = api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = "Расписание на турнир " + CupName + " @ " + datetime.ToString("dd.MM.yyyy") + " 20:00 bo3\r\n"
                 + loosers[0] + " vs " + lWinners[0] + " @ kTVCSS №1\r\n"
                  + loosers[1] + " vs " + lWinners[1] + " @ kTVCSS №2\r\n"
                  + "Правила турниров: https://v34.ktvcss.org.ru/rules.php\r\n" +
                      "По любым вопросам, относящимся к турниру, обращаться к @waneda_avganskaya(Waneda) или @jekacheater(SNAX).",
                FromGroup = true,
                Signed = false,
                Attachments = new List<MediaAttachment>
                {
                   photo.FirstOrDefault()
                }
            });

            InsertDataToRecovery("postId", PostId.ToString());

            UpdateCupStatus(CupName, 22);

        CupStatusId22:

            // 4rl
            while (!IsTimeToKick(GetPartDay("4rl")))
            {
                Thread.Sleep(30000);
            }

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId FROM cups_matches WHERE BracketItem = '4rl' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                KickFromServer(WorkClass.ServerList[int.Parse(reader[0].ToString())].Host, WorkClass.ServerList[int.Parse(reader[0].ToString())].GamePort.ToString(), WorkClass.ServerList[int.Parse(reader[0].ToString())].RconPassword);
            }
            connection.Close();

            UpdateCupStatus(CupName, 23);

        CupStatusId23:

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId, TeamAName, TeamBName, DateTimeStart, DateTimeEnd FROM cups_matches WHERE BracketItem = '4rl' AND TournamentId = '" + CupId + "' AND MatchPlayed = 0;", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                var check = new Thread(CheckTrueTeamNames);
                check.Start(reader[0].ToString() + ";" + reader[1].ToString() + ";" + reader[2].ToString() + ";" + reader[3].ToString() + ";" + reader[4].ToString() + ";4rl");
            }
            connection.Close();

            while (!CheckIsPartFinished(2, "4rl"))
            {
                Thread.Sleep(15000);
            }

            UpdateCupStatus(CupName, 24);

        CupStatusId24:

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();

            winners = new List<string>();
            loosers = new List<string>();
            datetime = new DateTime();

            // get 4rl for lbf
            query = new MySqlCommand("SELECT MatchWinnerName, DateTimeStart FROM cups_matches WHERE BracketItem = '4rl' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                winners.Add(reader[0].ToString());
                datetime = DateTime.Parse(reader[1].ToString());
            }
            reader.Close();

            datetime = datetime.AddDays(1);

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + winners[0] + "', '" + winners[1] + "', '" + CupId + "', '0', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', 'lbf', 'bo3');", connection);
            query.ExecuteNonQuery();

            connection.Close();

            k = 0;

            for (int i = 56; i < 58; i++)
            {
                Bracket.UpdateDbBracket(i + 1, winners[k]); // 5rl
                k++;
            }

            k = 0;

            Bracket.Draw();

            // linux path
            uploadServer = api.Photo.GetWallUploadServer(WorkClass.MainGroupId);
            result = Encoding.ASCII.GetString(Web.UploadFile(uploadServer.UploadUrl, @"Images\Cups\bracket_work.png"));
            photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.MainGroupId);

            PostId = api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = "Расписание на турнир " + CupName + " @ " + datetime.ToString("dd.MM.yyyy") + " 20:00 bo3\r\n"
                 + winners[0] + " vs " + winners[1] + " @ kTVCSS №1\r\n"
                  + "Правила турниров: https://v34.ktvcss.org.ru/rules.php\r\n" +
                      "По любым вопросам, относящимся к турниру, обращаться к @waneda_avganskaya(Waneda) или @jekacheater(SNAX).",
                FromGroup = true,
                Signed = false,
                Attachments = new List<MediaAttachment>
                {
                   photo.FirstOrDefault()
                }
            });

            InsertDataToRecovery("postId", PostId.ToString());

            UpdateCupStatus(CupName, 25);

        CupStatusId25:

            // lbf
            while (!IsTimeToKick(GetPartDay("lbf")))
            {
                Thread.Sleep(30000);
            }

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId FROM cups_matches WHERE BracketItem = 'lbf' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                KickFromServer(WorkClass.ServerList[int.Parse(reader[0].ToString())].Host, WorkClass.ServerList[int.Parse(reader[0].ToString())].GamePort.ToString(), WorkClass.ServerList[int.Parse(reader[0].ToString())].RconPassword);
            }
            connection.Close();

            UpdateCupStatus(CupName, 26);

        CupStatusId26:

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId, TeamAName, TeamBName, DateTimeStart, DateTimeEnd FROM cups_matches WHERE BracketItem = 'lbf' AND TournamentId = '" + CupId + "' AND MatchPlayed = 0;", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                var check = new Thread(CheckTrueTeamNames);
                check.Start(reader[0].ToString() + ";" + reader[1].ToString() + ";" + reader[2].ToString() + ";" + reader[3].ToString() + ";" + reader[4].ToString() + ";lbf");
            }
            connection.Close();

            while (!CheckIsPartFinished(1, "lbf"))
            {
                Thread.Sleep(15000);
            }

            UpdateCupStatus(CupName, 27);

        CupStatusId27:

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();

            winners = new List<string>();
            loosers = new List<string>();
            datetime = new DateTime();

            // get from ubf for lf
            query = new MySqlCommand("SELECT MatchWinnerName, MatchLooserName, DateTimeStart FROM cups_matches WHERE BracketItem = 'ubf' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                winners.Add(reader[0].ToString());
                datetime = DateTime.Parse(reader[2].ToString());
                loosers.Add(reader[1].ToString());
            }
            reader.Close();

            datetime = datetime.AddDays(1);

            // lower bracket final results
            lWinners = new List<string>();
            query = new MySqlCommand("SELECT MatchWinnerName, DateTimeStart FROM cups_matches WHERE BracketItem = 'lbf' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                lWinners.Add(reader[0].ToString());
                datetime = DateTime.Parse(reader[1].ToString());
            }
            reader.Close();

            datetime = datetime.AddDays(1);

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + loosers[0] + "', '" + lWinners[0] + "', '" + CupId + "', '0', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', 'lf', 'bo3');", connection);
            query.ExecuteNonQuery();

            connection.Close();

            Bracket.UpdateDbBracket(59, lWinners[0]);

            Bracket.Draw();

            // linux path
            uploadServer = api.Photo.GetWallUploadServer(WorkClass.MainGroupId);
            result = Encoding.ASCII.GetString(Web.UploadFile(uploadServer.UploadUrl, @"Images\Cups\bracket_work.png"));
            photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.MainGroupId);

            PostId = api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = "Расписание на турнир " + CupName + " @ " + datetime.ToString("dd.MM.yyyy") + " 20:00 bo3\r\n"
                 + loosers[0] + " vs " + lWinners[0] + " @ kTVCSS №1\r\n"
                  + "Правила турниров: https://v34.ktvcss.org.ru/rules.php\r\n" +
                      "По любым вопросам, относящимся к турниру, обращаться к @waneda_avganskaya(Waneda) или @jekacheater(SNAX).",
                FromGroup = true,
                Signed = false,
                Attachments = new List<MediaAttachment>
                {
                   photo.FirstOrDefault()
                }
            });

            InsertDataToRecovery("postId", PostId.ToString());

            UpdateCupStatus(CupName, 28);

        CupStatusId28:

            // lf
            while (!IsTimeToKick(GetPartDay("lf")))
            {
                Thread.Sleep(30000);
            }

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId FROM cups_matches WHERE BracketItem = 'lf' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                KickFromServer(WorkClass.ServerList[int.Parse(reader[0].ToString())].Host, WorkClass.ServerList[int.Parse(reader[0].ToString())].GamePort.ToString(), WorkClass.ServerList[int.Parse(reader[0].ToString())].RconPassword);
            }
            connection.Close();

            UpdateCupStatus(CupName, 29);

        CupStatusId29:

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId, TeamAName, TeamBName, DateTimeStart, DateTimeEnd FROM cups_matches WHERE BracketItem = 'lf' AND TournamentId = '" + CupId + "' AND MatchPlayed = 0;", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                var check = new Thread(CheckTrueTeamNames);
                check.Start(reader[0].ToString() + ";" + reader[1].ToString() + ";" + reader[2].ToString() + ";" + reader[3].ToString() + ";" + reader[4].ToString() + ";lf");
            }
            connection.Close();

            while (!CheckIsPartFinished(1, "lf"))
            {
                Thread.Sleep(15000);
            }

            UpdateCupStatus(CupName, 30);

        CupStatusId30:

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();

            winners = new List<string>();
            loosers = new List<string>();
            datetime = new DateTime();

            // get from ubf for gf
            query = new MySqlCommand("SELECT MatchWinnerName, MatchLooserName, DateTimeStart FROM cups_matches WHERE BracketItem = 'ubf' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                winners.Add(reader[0].ToString());
                datetime = DateTime.Parse(reader[2].ToString());
                loosers.Add(reader[1].ToString());
            }
            reader.Close();

            datetime = datetime.AddDays(1);

            // lower final results
            lWinners = new List<string>();
            query = new MySqlCommand("SELECT MatchWinnerName, DateTimeStart FROM cups_matches WHERE BracketItem = 'lf' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                lWinners.Add(reader[0].ToString());
                datetime = DateTime.Parse(reader[1].ToString());
            }
            reader.Close();

            datetime = datetime.AddDays(1);

            query = new MySqlCommand("INSERT INTO cups_matches (`TeamAName`, `TeamBName`, `TournamentId`, `ServerId`, `DateTimeStart`, `DateTimeEnd`, `BracketItem`, `MatchFormat`)" +
                " VALUES ('" + winners[0] + "', '" + lWinners[0] + "', '" + CupId + "', '0', '" + datetime.ToString("yyyy-MM-dd") + " 20:00:00', '"
                 + datetime.ToString("yyyy-MM-dd") + " 23:59:59', 'gf', 'bo5');", connection);
            query.ExecuteNonQuery();

            connection.Close();

            Bracket.UpdateDbBracket(60, lWinners[0]);

            Bracket.Draw();

            // linux path
            uploadServer = api.Photo.GetWallUploadServer(WorkClass.MainGroupId);
            result = Encoding.ASCII.GetString(Web.UploadFile(uploadServer.UploadUrl, @"Images\Cups\bracket_work.png"));
            photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.MainGroupId);

            PostId = api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = "Расписание на турнир " + CupName + " @ " + datetime.ToString("dd.MM.yyyy") + " 20:00 bo5\r\n"
                 + winners[0] + " vs " + lWinners[0] + " @ kTVCSS №1\r\n"
                  + "Правила турниров: https://v34.ktvcss.org.ru/rules.php\r\n" +
                      "По любым вопросам, относящимся к турниру, обращаться к @waneda_avganskaya(Waneda) или @jekacheater(SNAX).",
                FromGroup = true,
                Signed = false,
                Attachments = new List<MediaAttachment>
                {
                   photo.FirstOrDefault()
                }
            });

            InsertDataToRecovery("postId", PostId.ToString());

            UpdateCupStatus(CupName, 31);

        CupStatusId31:

            // gf
            while (!IsTimeToKick(GetPartDay("gf")))
            {
                Thread.Sleep(30000);
            }

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId FROM cups_matches WHERE BracketItem = 'gf' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                KickFromServer(WorkClass.ServerList[int.Parse(reader[0].ToString())].Host, WorkClass.ServerList[int.Parse(reader[0].ToString())].GamePort.ToString(), WorkClass.ServerList[int.Parse(reader[0].ToString())].RconPassword);
            }
            connection.Close();

            UpdateCupStatus(CupName, 32);

        CupStatusId32:

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            query = new MySqlCommand("SELECT ServerId, TeamAName, TeamBName, DateTimeStart, DateTimeEnd FROM cups_matches WHERE BracketItem = 'gf' AND TournamentId = '" + CupId + "' AND MatchPlayed = 0;", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                var check = new Thread(CheckTrueTeamNames);
                check.Start(reader[0].ToString() + ";" + reader[1].ToString() + ";" + reader[2].ToString() + ";" + reader[3].ToString() + ";" + reader[4].ToString() + ";gf");
            }
            connection.Close();

            while (!CheckIsPartFinished(1, "gf"))
            {
                Thread.Sleep(15000);
            }

            UpdateCupStatus(CupName, 33);

        CupStatusId33:

            connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();

            winners = new List<string>();
            loosers = new List<string>();
            datetime = new DateTime();

            // get from ubf for gf
            query = new MySqlCommand("SELECT MatchWinnerName, MatchLooserName, DateTimeStart FROM cups_matches WHERE BracketItem = 'gf' AND TournamentId = '" + CupId + "';", connection);
            reader = query.ExecuteReader();
            while (reader.Read())
            {
                winners.Add(reader[0].ToString());
                datetime = DateTime.Parse(reader[2].ToString());
                loosers.Add(reader[1].ToString());
            }
            reader.Close();
            
            connection.Close();

            // linux path

            Bracket.Draw();

            uploadServer = api.Photo.GetWallUploadServer(WorkClass.MainGroupId);
            result = Encoding.ASCII.GetString(Web.UploadFile(uploadServer.UploadUrl, @"Images\Cups\bracket_work.png"));
            photo = api.Photo.SaveWallPhoto(result, (ulong?)WorkClass.AdminUserId, (ulong?)WorkClass.MainGroupId);


            PostId = api.Wall.Post(new WallPostParams()
            {
                OwnerId = -WorkClass.MainGroupId,
                Message = CupName + " @ Итоговая сетка\r\nКоманда " + winners[0] + " побеждает в гранд-финале!\r\nПодведение итогов турнира будет в следующем посте!",
                FromGroup = true,
                Signed = false,
                Attachments = new List<MediaAttachment>
                {
                   photo.FirstOrDefault()
                }
            });

            UpdateCupStatus(CupName, 34);

        CupStatusId34:
            End();
            WorkClass.PrintLogMessage("CUP ENDED", "INFO");
        }

        private static void ExecuteNonQuery(string query)
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var cmd = new MySqlCommand(query, connection);
            cmd.ExecuteNonQuery();
        }

        private static void End()
        {
            ExecuteNonQuery("UPDATE cups_bracket SET TEXT = '' WHERE TEXT != ''");
            ExecuteNonQuery($"UPDATE cups_list SET ISFINISHED = 1 WHERE ID = {CupId}");
            ExecuteNonQuery("TRUNCATE TABLE cups_matches");
            ExecuteNonQuery("UPDATE cups_recovery SET RegBoardId = '0', CupId = '0', Teams = '0', postId = '0', password = '0', CupName = '0'");
            ExecuteNonQuery("TRUNCATE TABLE cups_teams");
        }

        private static bool CheckIsPartFinished(int playCountNeedle, string partName)
        {
            var playCount = 0;
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT MatchPlayed FROM cups_matches WHERE TournamentId = '" + CupId + "' AND BracketItem = '" + partName.ToString() + "';", connection);
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
            while (true)
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                // steams check

                var stuffFromDb = GetSteams(tTrueName, ctTrueName);
                var stuffFromServer = webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=sm_kurwagay").Split('\n');
                stuffFromServer[0] = stuffFromServer[0].Substring(stuffFromServer[0].IndexOf("\"") + 1);
                foreach (var item in stuffFromServer)
                {
                    if (!item.Contains("STEAM")) continue;
                    var steam = item.Split(';');
                    if (!stuffFromDb.Contains(steam[1]))
                    {
                        webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + 
                        "&port=" + WorkClass.ServerList[serverId].GamePort + 
                            "&password=" + WorkClass.ServerList[serverId].RconPassword + 
                        "&command=kickid " + steam[0] + " Access on this match is denied for you");
                    }
                }

                // next

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
                        webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=say [kTVCSSBot] Пожалуйста, поставьте правильные теги команд, иначе результат матча не будет учтен");
                        Thread.Sleep(500);
                        webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=say [kTVCSSBot] Правильный тег команды А: " + tTrueName);
                        Thread.Sleep(500);
                        webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=say [kTVCSSBot] Правильный тег команды Б: " + ctTrueName);
                    }
                }
                else
                {
                    WorkClass.PrintLogMessage("INCORRECT TAGS " + tRegex + " - " + tTrueName + "; " + ctRegex + " - " + ctTrueName, "DEBUG");
                    webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=say [kTVCSSBot] Пожалуйста, поставьте правильные теги команд, иначе результат матча не будет учтен");
                    Thread.Sleep(500);
                    webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=say [kTVCSSBot] Правильный тег команды А: " + tTrueName);
                    Thread.Sleep(500);
                    webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + WorkClass.ServerList[serverId].Host + "&port=" + WorkClass.ServerList[serverId].GamePort + "&password=" + WorkClass.ServerList[serverId].RconPassword + "&command=say [kTVCSSBot] Правильный тег команды Б: " + ctTrueName);
                }

            EmptyTag:

                if (PlayedMatchesHook(ctTrueName, tTrueName, dateFrom, dateTo, tPart))
                {
                    return;
                }

                Thread.Sleep(30000);
            }
        }

        private static string GetSteams(string teamA, string teamB)
        {
            var stuff = "";
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand($"SELECT TSTUFF FROM cups_teams WHERE TNAME = '{teamA}' OR TNAME = '{teamB}'", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                stuff += reader[0].ToString();
            }
            return stuff;
        }

        private static bool IsMatchProcessed(int id)
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand($"SELECT ID FROM cups_matchesplayed WHERE ID = {id}", connection);
            var reader = query.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Close();
                connection.Close();
                return true;
            }
            return false;
        }

        private static void SetMatchProcessed(int id)
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand($"INSERT INTO cups_matchesplayed VALUES ({id})", connection);
            query.ExecuteNonQuery();
            connection.Close();
        }

        private static bool BestOfThreeCase(string tName, string ctName, string tPart, bool tWin)
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand($"SELECT TeamAScore, TeamBScore, TeamAName, TeamBName FROM cups_matches " +
            	"WHERE ((TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "') " +
            	"OR (TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "')) " +
            	"AND TournamentId = '" + CupId + "' AND BracketItem = '" + tPart + "';", connection);
            var reader = query.ExecuteReader();

            var tScore = 0;
            var ctScore = 0;
            var tDbName = "";
            var ctDbName = "";

            while (reader.Read())
            {
                tScore = int.Parse(reader[0].ToString());
                ctScore = int.Parse(reader[1].ToString());
                tDbName = reader[2].ToString();
                ctDbName = reader[3].ToString();
            }
            reader.Close();

            if (tWin)
            {
                if (tName == tDbName)
                    tScore++;
                else ctScore++;
            }
            else
            {
                if (ctName == ctDbName)
                    ctScore++;
                else tScore++;
            }

            if (tScore > ctScore)
            {
                var updateQuery = new MySqlCommand($"UPDATE cups_matches SET TeamAScore = {tScore}, TeamBScore = {ctScore} " +
                "WHERE ((TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "') " +
                "OR (TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "')) " +
                "AND TournamentId = '" + CupId + "' AND BracketItem = '" + tPart + "';", connection);
                updateQuery.ExecuteNonQuery();

                if ((tScore == 2 && ctScore == 0) || (tScore == 2 && ctScore == 1))
                {
                    connection.Close();
                    return true;
                }
            }

            if (tScore < ctScore)
            {
                var updateQuery = new MySqlCommand($"UPDATE cups_matches SET TeamAScore = {tScore}, TeamBScore = {ctScore} " +
                "WHERE ((TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "') " +
                "OR (TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "')) " +
                "AND TournamentId = '" + CupId + "' AND BracketItem = '" + tPart + "';", connection);
                updateQuery.ExecuteNonQuery();

                if ((ctScore == 2 && tScore == 0) || (ctScore == 2 && tScore == 1))
                {
                    connection.Close();
                    return true;
                }
            }

            if (tScore == ctScore)
            {
                var updateQuery = new MySqlCommand($"UPDATE cups_matches SET TeamAScore = {tScore}, TeamBScore = {ctScore} " +
                "WHERE ((TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "') " +
                "OR (TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "')) " +
                "AND TournamentId = '" + CupId + "' AND BracketItem = '" + tPart + "';", connection);
                updateQuery.ExecuteNonQuery();
            }

            connection.Close();
            return false;
        }

        private static bool BestOfFiveCase(string tName, string ctName, string tPart, bool tWin)
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand($"SELECT TeamAScore, TeamBScore, TeamAName, TeamBName FROM cups_matches " +
                "WHERE ((TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "') " +
                "OR (TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "')) " +
                "AND TournamentId = '" + CupId + "' AND BracketItem = '" + tPart + "';", connection);
            var reader = query.ExecuteReader();

            var tScore = 0;
            var ctScore = 0;
            var tDbName = "";
            var ctDbName = "";

            while (reader.Read())
            {
                tScore = int.Parse(reader[0].ToString());
                ctScore = int.Parse(reader[1].ToString());
                tDbName = reader[2].ToString();
                ctDbName = reader[3].ToString();
            }
            reader.Close();

            if (tWin)
            {
                if (tName == tDbName)
                    tScore++;
                else ctScore++;
            }
            else
            {
                if (ctName == ctDbName)
                    ctScore++;
                else tScore++;
            }

            if (tScore > ctScore)
            {
                var updateQuery = new MySqlCommand($"UPDATE cups_matches SET TeamAScore = {tScore}, TeamBScore = {ctScore} " +
                "WHERE ((TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "') " +
                "OR (TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "')) " +
                "AND TournamentId = '" + CupId + "' AND BracketItem = '" + tPart + "';", connection);
                updateQuery.ExecuteNonQuery();

                if ((tScore == 3 && ctScore == 0) || (tScore == 3 && ctScore == 1) || (tScore == 3 && ctScore == 2))
                {
                    connection.Close();
                    return true;
                }
            }

            if (tScore < ctScore)
            {
                var updateQuery = new MySqlCommand($"UPDATE cups_matches SET TeamAScore = {tScore}, TeamBScore = {ctScore} " +
                "WHERE ((TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "') " +
                "OR (TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "')) " +
                "AND TournamentId = '" + CupId + "' AND BracketItem = '" + tPart + "';", connection);
                updateQuery.ExecuteNonQuery();

                if ((ctScore == 3 && tScore == 0) || (ctScore == 3 && tScore == 1) || (ctScore == 3 && tScore == 2))
                {
                    connection.Close();
                    return true;
                }
            }

            if (tScore == ctScore)
            {
                var updateQuery = new MySqlCommand($"UPDATE cups_matches SET TeamAScore = {tScore}, TeamBScore = {ctScore} " +
                "WHERE ((TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "') " +
                "OR (TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "')) " +
                "AND TournamentId = '" + CupId + "' AND BracketItem = '" + tPart + "';", connection);
                updateQuery.ExecuteNonQuery();
            }

            connection.Close();
            return false;
        }

        private static bool PlayedMatchesHook(string ct, string t, string dateFrom, string dateTo, string tPart)
        {
            // case
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            var query = new MySqlCommand("SELECT TeamA, TeamAScore, TeamB, TeamBScore, ID FROM matches WHERE MatchDate > '" + DateTime.Parse(dateFrom).ToString("yyyy-MM-dd HH:mm:ss") + "' AND MatchDate < '" + DateTime.Parse(dateTo).ToString("yyyy-MM-dd HH:mm:ss") + "';", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                if ((reader[0].ToString() == t && reader[2].ToString() == ct) || (reader[0].ToString() == ct && reader[2].ToString() == t))
                {
                    var matchId = reader[4].ToString();
                    var tScore = int.Parse(reader[1].ToString());
                    var ctScore = int.Parse(reader[3].ToString());
                    var tName = reader[0].ToString();
                    var ctName = reader[2].ToString();
                    if (!IsMatchProcessed(int.Parse(matchId)))
                    {
                        if (tScore > ctScore)
                        {
                            if (tPart == "gf")
                            {
                                if (BestOfFiveCase(tName, ctName, tPart, true))
                                {
                                    SetMatchFinished(true, tName, ctName, tPart);
                                    return true;
                                }
                            }
                            else
                            {
                                if (BestOfThreeCase(tName, ctName, tPart, true))
                                {
                                    SetMatchFinished(true, tName, ctName, tPart);
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            //reader.Close();
                            if (tPart == "gf")
                            {
                                if (BestOfFiveCase(tName, ctName, tPart, false))
                                {
                                    SetMatchFinished(false, tName, ctName, tPart);
                                    return true;
                                }
                            }
                            else
                            {
                                if (BestOfThreeCase(tName, ctName, tPart, false))
                                {
                                    SetMatchFinished(false, tName, ctName, tPart);
                                    return true;
                                }
                            }
                        }
                        SetMatchProcessed(int.Parse(matchId));
                    }
                }
            }
            connection.Close();
            return false;
        }

        private static void SetMatchFinished(bool tWin, string tName, string ctName, string tPart)
        {
            var connection = new MySqlConnection(WorkClass.ConnectionString);
            connection.Open();
            if (tWin)
            {
                var insertQuery = new MySqlCommand("UPDATE cups_matches SET MatchPlayed = '1', MatchWinnerName = '" + tName + "', MatchLooserName = '" + ctName + "' WHERE ((TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "') OR (TeamAName = '" + ctName + "' OR TeamAName = '" + tName + "')) AND TournamentId = '" + CupId + "' AND BracketItem = '" + tPart + "';", connection);
                insertQuery.ExecuteNonQuery();
            }
            else
            {
                var insertQuery = new MySqlCommand("UPDATE cups_matches SET MatchPlayed = '1', MatchWinnerName = '" + ctName + "', MatchLooserName = '" + tName + "' WHERE ((TeamAName = '" + tName + "' OR TeamAName = '" + ctName + "') OR (TeamAName = '" + ctName + "' OR TeamAName = '" + tName + "')) AND TournamentId = '" + CupId + "' AND BracketItem = '" + tPart + "';", connection);
                insertQuery.ExecuteNonQuery();
            }
            connection.Close();
        }

        private static void KickFromServer(string host, string port, string rconPassword)
        {
            var webClient = new WebClient();
            try
            {
                webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host + "&port=" + port + "&password=" + rconPassword + "&command=say [kTVCSSBot] Через 5 минут начинается турнир на этом сервере");
                Thread.Sleep(1000);
                webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host + "&port=" + port + "&password=" + rconPassword + "&command=say [kTVCSSBot] Приносим извинения, если вы не успели доиграть матч");
                Thread.Sleep(3000);
                webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host + "&port=" + port + "&password=" + rconPassword + "&command=cm");
                Thread.Sleep(1000);
                webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host + "&port=" + port + "&password=" + rconPassword + "&command=sm_kick @ct The server is busy by the project!");
                webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host + "&port=" + port + "&password=" + rconPassword + "&command=sm_kick @t The server is busy by the project!");
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

        private static bool IsTimeToKick(DateTime date)
        {
            if (date.Day == DateTime.Now.Day)
            {
                if (DateTime.Now.Hour == 18 && DateTime.Now.Minute == 55) return true;
                else return false;
            }
            else return false;
        }

        private static bool VerifyEntry(string entryText, long fromId)
        {
            if (!entryText.Contains("1."))
            {
                SendErrorToUser(fromId, "Вы указали неверный формат заявки. Обязательно необходимо указать название команды" +
                    " в формате 1. Название команды (например, 1. dk.gaming)");
                return false;
            }

            var steamIds = Regex.Matches(entryText, @"STEAM_[0-5]:[01]:\d+", RegexOptions.Multiline);
            var entrySteams = new List<string>();
            foreach (var steam in steamIds)
            {
                entrySteams.Add(steam.ToString());
            }
            if (entrySteams.Count > 5)
                return true;
            else
            {
                SendErrorToUser(fromId, "Вы указали менее пяти игроков и/или вы не указали как минимум 5 стим идентификаторов");
            }
            return false;
        }

        private static void SendErrorToUser(long fromId, string text)
        {
            var api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = WorkClass.Token,
            });

            try
            {
                api.Messages.Send(new MessagesSendParams()
                {
                    GroupId = (ulong)WorkClass.MainGroupId,
                    PeerId = fromId,
                    Message = text,
                    RandomId = new Random().Next()
                });
            }
            catch (Exception)
            {
                try
                {
                    api.Messages.Send(new MessagesSendParams()
                    {
                        PeerId = fromId,
                        Message = text,
                        RandomId = new Random().Next()
                    });
                }
                catch (Exception)
                {
                    // Ignored
                }
            }
        }
    }
}
