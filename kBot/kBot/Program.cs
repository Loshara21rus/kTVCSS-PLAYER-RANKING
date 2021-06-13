using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.RequestParams;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;

using System.IO;
using MySql.Data.MySqlClient;
using System.Net;
using Newtonsoft.Json;

namespace kBot
{
    class TableType
    {
        public const string Users = "Users";
        public const string Teams = "Teams";
        public const string Rooms = "Rooms";
    }

    class Program
    {
        private static List<long> answeredMessages = new List<long>();
        private static List<long> answeredMessagesCuzImOnWork = new List<long>();
        private static string tsAnswer = "Здравствуйте! Я kurwanatorVkBot! Я вижу, что Вы обратились по поводу нашего TeamSpeak3 сервера.\nЕсли Вам необходимо загрузить значок на сервер или купить випку, то обратитесь к одному из админов:\n@eeelnaraaa\n@vra4ixaaa\n@mambo_gg\n@asmadey_sa\nЗначок должен быть 16х16 или 32х32 пикселей с прозрачным фоном. Сделать это можно в интернете или с помощью Adobe Photoshop. Можно, конечно, попросить админов об этом, но они будут Вам признательны, если Вы это сделаете сами.\nЧтобы посмотреть цены и описание вип-привилегий, перейдите по ссылке: https://vk.com/topic-194508284_40575309\nЕсли Вам нужна ссылка на скачивание, то вот:\nhttps://www.teamspeak.com/ru/\nЕсли у Вас другой вопрос, обращайтесь к админам, указанным выше!";
        private static string banAnswer = "Здравствуйте! Я kurwanatorVkBot! Я вижу, что Вы обратились по поводу бана.\nЕсли у Вас бан на игровом сервере, то Вам необходимо посмотреть причину бана на сайте SourceBans: https://v34.ktvcss.org.ru/sourcebans/ и написать об этом @inzame\nЕсли у Вас бан на TeamSpeak сервере, то обращайтесь к админу, который Вас забанил, нечего мне жаловаться об этом!\n\n@eeelnaraaa\n@vra4ixaaa\n@myvy111\n@mambo_gg";
        private static string imOnWork = "Здравствуйте! Я kurwanatorVkBot! В данный момент меня нет дома - я на работе и без телефона. Пожалуйста, напишите мне по интересующему Вас вопросу после 18:00 по Москве. Когда я приду с работы, постараюсь ответить. Хорошего дня!";
        private static string cmAnswer = "Здравствуйте! Я kurwanatorVkBot! Я вижу, что Вы обратились насчет клиентмода. Я не разработчик км-а, но я постараюсь немного Вам помочь.\nЕсли у Вас возник вопрос по кму, то лучше всего сначала найти на него ответ на официальном форуме: https://clientmod.ru/forum/threads/18/\nЕсли Вы ничего не нашли, то попробуйте написать разработчику: @reg1oxen";
        private static string demoAnswer = "Здравствуйте! Я kurwanatorVkBot! Я вижу, что Вы обратились насчет демок с ктв серверов.\nСервер №1: http://web.cw-serv.ru/id3287/\nСервер №2: http://web.cw-serv.ru/id3284/\nСервер №3: http://web.cw-serv.ru/id3285/\nСервер №4: http://web.cw-serv.ru/id3286/";
        private static string bidloAnswer = "ОСУЖДАЮ БЫДЛО";
        private static string iSleep = "Здравствуйте! Я kurwanatorVkBot! В это время я сплю, так что пишите вечером!";
        private static string kickAnswer = "Здравствуйте! Я kurwanatorVkBot! Я вижу, что Вы обратились насчет кика с серверов.\nЧтобы кикнуть игрока с сервера, Вы можете написать votekick на сервере. Или же обратитесь к @inzame.";
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "ktvcssTable";
        static UserCredential credential;
        static string dbCon = "server=localhost;user=root;password=;database=kTVCSS;";

        private static void VaryaAlerter()
        {
            var api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = "tokenhere"
            });
            while (true)
            {
                if (DateTime.Now.Hour == 11 && DateTime.Now.Minute == 30)
                {
                    try
                    {
                        api.Messages.Send(new MessagesSendParams()
                        {
                            PeerId = 440952613,
                            Message = "Любимая, проверь, пожалуйста, сделан ли у тебя пост на 12 часов <3",
                            RandomId = new Random().Next()
                        });

                        api.Messages.Send(new MessagesSendParams()
                        {
                            PeerId = 226584459,
                            Message = "Любимая, проверь, пожалуйста, сделан ли у тебя пост на 12 часов <3",
                            RandomId = new Random().Next()
                        });
                    }
                    catch (Exception)
                    {
                        // Ignored
                    }
                }

                if (DateTime.Now.Hour == 13 && DateTime.Now.Minute == 30)
                {
                    try
                    {
                        api.Messages.Send(new MessagesSendParams()
                        {
                            PeerId = 440952613,
                            Message = "Любимая, проверь, пожалуйста, сделан ли у тебя пост на 14 часов <3",
                            RandomId = new Random().Next()
                        });

                        api.Messages.Send(new MessagesSendParams()
                        {
                            PeerId = 226584459,
                            Message = "Любимая, проверь, пожалуйста, сделан ли у тебя пост на 14 часов <3",
                            RandomId = new Random().Next()
                        });
                    }
                    catch (Exception)
                    {
                        // Ignored
                    }
                }

                if (DateTime.Now.Hour == 17 && DateTime.Now.Minute == 30)
                {
                    try
                    {
                        api.Messages.Send(new MessagesSendParams()
                        {
                            PeerId = 440952613,
                            Message = "Любимая, проверь, пожалуйста, сделан ли у тебя пост на 18 часов <3",
                            RandomId = new Random().Next()
                        });

                        api.Messages.Send(new MessagesSendParams()
                        {
                            PeerId = 226584459,
                            Message = "Любимая, проверь, пожалуйста, сделан ли у тебя пост на 18 часов <3",
                            RandomId = new Random().Next()
                        });
                    }
                    catch (Exception)
                    {
                        // Ignored
                    }
                }
                Thread.Sleep(60000);
            }
        }

        public static List<Servers> _serverList = new List<Servers>();

        public struct Servers
        {
            public string host;
            public string userName;
            public string userPassword;
            public int port;
            public string logsDir;
            public int gamePort;
            public string rconPassword;

            public Servers(string host, string userName, string userPassword, int port, string logsDir, int gamePort, string rconPassword)
            {
                this.host = host;
                this.userName = userName;
                this.userPassword = userPassword;
                this.port = port;
                this.logsDir = logsDir;
                this.gamePort = gamePort;
                this.rconPassword = rconPassword;
            }
        }
        //;password=Qwe1337
        private static void LoadConfig()
        {
            var connection = new MySqlConnection(dbCon);
            connection.Open();
            var query = new MySqlCommand("SELECT * FROM servers", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                var server = new Servers
                {
                    host = reader[2].ToString(),
                    userName = reader[3].ToString(),
                    userPassword = reader[4].ToString(),
                    port = int.Parse(reader[5].ToString()),
                    logsDir = reader[6].ToString(),
                    gamePort = int.Parse(reader[7].ToString()),
                    rconPassword = reader[8].ToString()
                };
                _serverList.Add(server);
            }
            connection.Close();
        }

        private static void BotProcess()
        {
            var api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = "tokenhere"
            });
            while (true)
            {
                try
                {
                    var lastDialog = api.Messages.GetConversations(new GetConversationsParams()).Items[0];
                    if (lastDialog.Conversation.Peer.Type == ConversationPeerType.User)
                    {
                        var message = lastDialog.LastMessage.Text.ToLower();
                        if (message.StartsWith("!rconexec"))
                        {
                            var cmd = lastDialog.LastMessage.Text.Substring(10);
                            var query = cmd.Split(';');
                            var web = new WebClient() { Encoding = Encoding.UTF8 };
                            var answer = web.DownloadString("https://alt.ktvcss.org.ru/ktvac/rcon.php?address=" + _serverList[int.Parse(query[0])].host + "&port=" + _serverList[int.Parse(query[0])].gamePort + "&password=" + _serverList[int.Parse(query[0])].rconPassword + "&command=" + query[1]);
                            api.Messages.Send(new MessagesSendParams()
                            {
                                PeerId = lastDialog.LastMessage.FromId,
                                Message = "[rconQueryResult] " + answer,
                                RandomId = new Random().Next()
                            });
                            answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                        }
                        if (message.StartsWith("!worknodereload"))
                        {
                            var worknode = Process.GetProcessesByName("WorkNode");
                            foreach (var process in worknode)
                            {
                                process.Kill();
                            }
                            Thread.Sleep(5000);
                            worknode = Process.GetProcessesByName("WorkNode");
                            api.Messages.Send(new MessagesSendParams()
                            {
                                PeerId = lastDialog.LastMessage.FromId,
                                Message = "[WorkNode] Количество запущенных процессов WorkNode " + worknode.Count(),
                                RandomId = new Random().Next()
                            });
                            answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                        }
                        if (message.StartsWith("!ccw"))
                        {
                            var worknode = Process.GetProcessesByName("CustomCupWorker");
                            foreach (var process in worknode)
                            {
                                process.Kill();
                            }
                            Thread.Sleep(5000);
                            worknode = Process.GetProcessesByName("CustomCupWorker");
                            api.Messages.Send(new MessagesSendParams()
                            {
                                PeerId = lastDialog.LastMessage.FromId,
                                Message = "[CustomCupWorker] Количество запущенных процессов CustomCupWorker " + worknode.Count(),
                                RandomId = new Random().Next()
                            });
                            answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                        }
                        if (message.StartsWith("!sqlselectquery"))
                        {
                            try
                            {
                                var cmd = lastDialog.LastMessage.Text.Substring(16);
                                var connection = new MySqlConnection(dbCon);
                                connection.Open();
                                var query = new MySqlCommand(cmd, connection);
                                var reader = query.ExecuteReader();
                                var answer = "\r\n";
                                while (reader.Read())
                                {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        answer += reader[i].ToString() + "\t";
                                    }
                                    answer += "\r\n";
                                }
                                connection.Close();
                                api.Messages.Send(new MessagesSendParams()
                                {
                                    PeerId = lastDialog.LastMessage.FromId,
                                    Message = "[sqlSelectQuery]" + answer,
                                    RandomId = new Random().Next()
                                });
                            }
                            catch (Exception exp)
                            {
                                api.Messages.Send(new MessagesSendParams()
                                {
                                    PeerId = lastDialog.LastMessage.FromId,
                                    Message = "[sqlSelectQuery] " + exp.Message,
                                    RandomId = new Random().Next()
                                });
                            }
                            answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                        }
                        if (message.StartsWith("!sqlquery"))
                        {
                            try
                            {
                                var cmd = lastDialog.LastMessage.Text.Substring(10);
                                var connection = new MySqlConnection(dbCon);
                                connection.Open();
                                var query = new MySqlCommand(cmd, connection);
                                var qount = query.ExecuteNonQuery();
                                connection.Close();
                                api.Messages.Send(new MessagesSendParams()
                                {
                                    PeerId = lastDialog.LastMessage.FromId,
                                    Message = "[sqlQuery] " + qount + " rows affected",
                                    RandomId = new Random().Next()
                                });
                            }
                            catch (Exception exp)
                            {
                                api.Messages.Send(new MessagesSendParams()
                                {
                                    PeerId = lastDialog.LastMessage.FromId,
                                    Message = "[sqlQuery] " + exp.Message,
                                    RandomId = new Random().Next()
                                });
                            }
                            answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                        }
                        if (message.Contains("тимспик") || message.Contains("значок") || message.Contains("значек") || message.Contains("ts") || message.Contains("вип") || message.Contains("комнат"))
                        {
                            if (!answeredMessages.Contains(lastDialog.Conversation.LastMessageId) && lastDialog.LastMessage.FromId != 467651511)
                            {
                                api.Messages.Send(new MessagesSendParams()
                                {
                                    PeerId = lastDialog.LastMessage.FromId,
                                    Message = tsAnswer,
                                    RandomId = new Random().Next()
                                });
                                answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                            }
                        }
                        if (message.Contains("бан"))
                        {
                            if (!answeredMessages.Contains(lastDialog.Conversation.LastMessageId) && lastDialog.LastMessage.FromId != 467651511)
                            {
                                api.Messages.Send(new MessagesSendParams()
                                {
                                    PeerId = lastDialog.LastMessage.FromId,
                                    Message = banAnswer,
                                    RandomId = new Random().Next()
                                });
                                answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                            }
                        }
                        if (DateTime.Now.Hour >= 7 && DateTime.Now.Hour <= 16 && DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
                        {
                            if (!answeredMessages.Contains(lastDialog.Conversation.LastMessageId) && lastDialog.LastMessage.FromId != 467651511 && !answeredMessagesCuzImOnWork.Contains((long)lastDialog.LastMessage.FromId))
                            {
                                api.Messages.Send(new MessagesSendParams()
                                {
                                    PeerId = lastDialog.LastMessage.FromId,
                                    Message = imOnWork,
                                    RandomId = new Random().Next()
                                });
                                answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                                answeredMessagesCuzImOnWork.Add((long)lastDialog.LastMessage.FromId);
                            }
                        }
                        if (DateTime.Now.Hour >= 1 && DateTime.Now.Hour <= 7)
                        {
                            if (!answeredMessages.Contains(lastDialog.Conversation.LastMessageId) && lastDialog.LastMessage.FromId != 467651511 && !answeredMessagesCuzImOnWork.Contains((long)lastDialog.LastMessage.FromId))
                            {
                                api.Messages.Send(new MessagesSendParams()
                                {
                                    PeerId = lastDialog.LastMessage.FromId,
                                    Message = iSleep,
                                    RandomId = new Random().Next()
                                });
                                answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                                answeredMessagesCuzImOnWork.Add((long)lastDialog.LastMessage.FromId);
                            }
                        }
                        if (message.Contains("км") || message.Contains("клиентмод"))
                        {
                            if (!answeredMessages.Contains(lastDialog.Conversation.LastMessageId) && lastDialog.LastMessage.FromId != 467651511)
                            {
                                api.Messages.Send(new MessagesSendParams()
                                {
                                    PeerId = lastDialog.LastMessage.FromId,
                                    Message = cmAnswer,
                                    RandomId = new Random().Next()
                                });
                                answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                            }
                        }
                        if (message.Contains("демо") || message.Contains("демк"))
                        {
                            if (!answeredMessages.Contains(lastDialog.Conversation.LastMessageId) && lastDialog.LastMessage.FromId != 467651511)
                            {
                                api.Messages.Send(new MessagesSendParams()
                                {
                                    PeerId = lastDialog.LastMessage.FromId,
                                    Message = demoAnswer,
                                    RandomId = new Random().Next()
                                });
                                answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                            }
                        }
                        if (message.Contains("кикни") || message.Contains("кикнуть"))
                        {
                            if (!answeredMessages.Contains(lastDialog.Conversation.LastMessageId) && lastDialog.LastMessage.FromId != 467651511)
                            {
                                api.Messages.Send(new MessagesSendParams()
                                {
                                    PeerId = lastDialog.LastMessage.FromId,
                                    Message = kickAnswer,
                                    RandomId = new Random().Next()
                                });
                                answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                            }
                        }
                        if (message.Contains("усатый") || (message.Contains("ебал") && message.Contains("мать")) || (message.Contains("шлюх") && message.Contains("мать")))
                        {
                            if (!answeredMessages.Contains(lastDialog.Conversation.LastMessageId) && lastDialog.LastMessage.FromId != 467651511)
                            {
                                api.Messages.Send(new MessagesSendParams()
                                {
                                    PeerId = lastDialog.LastMessage.FromId,
                                    Message = bidloAnswer,
                                    RandomId = new Random().Next()
                                });
                                answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                                api.Account.Ban((long)lastDialog.LastMessage.FromId);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now + " @ " + ex.Message);
                }
                Thread.Sleep(2000);
            }
        }

        private static void Cleaner()
        {
            while (true)
            {
                if (DateTime.Now.Hour == 18)
                {
                    answeredMessagesCuzImOnWork.Clear();
                }
                Thread.Sleep(3 * 60 * 60 * 1000);
            }
        }

        private static void GoogleShit()
        {
            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "1088979244416-t0dno1bqi43mbb12dcihb8hoehnif61n.apps.googleusercontent.com",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }
        }

        private static void GoogleShit(string tableType, string[] rows)
        {
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var spreadsheetId = "1KHo-PUWkL7JPc6G-k1BMD5gu2-Qsk2W0mvGDBl8fCOM";
            var range = tableType.ToString() + "!A2:E";

            var values = new List<IList<object>>();
            var obj = new List<Object>();

            obj.AddRange(rows);

            values.Add(obj);

            var request = service.Spreadsheets.Values.Append(new ValueRange() { Values = values }, spreadsheetId, range);
            request.Credential = credential;
            request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

            var response = request.Execute();

            Console.WriteLine(JsonConvert.SerializeObject(response));
        }

        private static void tsVkBot()
        {
            var api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = "tokenhere"
            });
            while (true)
            {
                try
                {
                    var lastDialog = api.Messages.GetConversations(new GetConversationsParams()).Items[0];
                    if (lastDialog.Conversation.Peer.Type == ConversationPeerType.Chat)
                    {
                        var message = lastDialog.LastMessage.Text;
                        if (message.StartsWith("!qiwi"))
                        {
                            QiwiProcess(lastDialog.Conversation.LastMessageId);
                        }
                        if (message.StartsWith("!add"))
                        {
                            var rows = message.Split(';');
                            var fields = "";
                            foreach (var item in rows.Skip(1).ToArray())
                            {
                                fields += item + "\r\n";
                            }
                            var reportMessage = "[tsAdminVkBot] Вставлена строка в лист " + rows[0].Replace("!add ", null) + ": \r\n" + fields;
                            try
                            {
                                GoogleShit(rows[0].Replace("!add ", null), rows.Skip(1).ToArray());
                            }
                            catch (Exception exp)
                            {
                                Console.WriteLine(exp.Message);
                                api.Messages.Send(new MessagesSendParams()
                                {
                                    PeerId = 2000000078,
                                    Message = "[tsAdminVkBot] ОШИБКА: \r\n" + exp.Message + "\r\n" + exp.StackTrace + "\r\n" + exp.InnerException,
                                    RandomId = new Random().Next()
                                });
                                answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                            }
                            if (!answeredMessages.Contains(lastDialog.Conversation.LastMessageId))
                            {
                                api.Messages.Send(new MessagesSendParams()
                                {
                                    PeerId = 2000000078,
                                    Message = reportMessage,
                                    RandomId = new Random().Next()
                                });
                                answeredMessages.Add(lastDialog.Conversation.LastMessageId);
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
                Thread.Sleep(2000);
            }
        }

        private async static void QiwiProcess(long lastmessageId)
        {
            var qiwi = new QiwiApi.Qiwi("+79208126641", "4f5c03d4430c3000b9319e0338c6a8f2");
            var history = await qiwi.GetHistoryAsync();
            var resultString = "";
            var count = 0;
            foreach (var payment in history.Payments)
            {
                if (count == 5) continue;
                var payType = "";
                if (payment.Type == QiwiApi.Models.Enums.PaymentType.In)
                    payType = "Входящий";
                if (payment.Type == QiwiApi.Models.Enums.PaymentType.Out)
                    payType = "Исходящий";
                resultString += $"{payType} платеж от {payment.Date}:\r\nИсточник: {payment.Account}\r\nСумма: Sum - {payment.Sum} / Total - {payment.Total}\r\nСтатус платежа: {payment.Status}\r\nКомиссия: {payment.Comission}\r\nКомментарий: {payment.Comment}\r\n\r\n";
                count++;
            }
            var api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = "tokenhere"
            });
            if (!answeredMessages.Contains(lastmessageId))
            {
                try
                {
                    api.Messages.Send(new MessagesSendParams()
                    {
                        PeerId = 2000000078,
                        Message = resultString,
                        RandomId = new Random().Next()
                    });
                    answeredMessages.Add(lastmessageId);
                }
                catch (Exception)
                {
                    // Ignored
                }
            }
        }

        static void Main(string[] args)
        {
            LoadConfig();

            var Varya = new Thread(VaryaAlerter);
            Varya.Start();

            var botmain = new Thread(BotProcess);
            botmain.Start();

            var cleaner = new Thread(Cleaner);
            cleaner.Start();

            var googleShit = new Thread(GoogleShit);
            googleShit.Start();

            var tsVk = new Thread(tsVkBot);
            tsVk.Start();

            //var api = new VkApi();
            //api.Authorize(new ApiAuthParams
            //{
            //    AccessToken = "tokenhere"
            //});
            //FriendsGetParams friendsGetParams = new FriendsGetParams();
            //var friendList = api.Friends.Get(friendsGetParams);
            //foreach (var friend in friendList)
            //{
            //    try
            //    {
            //        api.Messages.Send(new MessagesSendParams()
            //        {
            //            PeerId = friend.Id,
            //            Message = "С Новым 2021 годом! Счастья, здоровья и всего самого лучшего!",
            //            RandomId = new Random().Next()
            //        });
            //    }
            //    catch (Exception)
            //    {
            //        // Ignored
            //    }
            //    Thread.Sleep(3000);
            //}
        }
    }
}
