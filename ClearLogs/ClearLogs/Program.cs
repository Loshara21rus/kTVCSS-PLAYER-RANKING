using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace ClearLogs
{
    class Program
    {
        private static string ConnectionString;
        public static List<Servers> ServerList = new List<Servers>();

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
            connection.Close();
        }

        private static void PrintLogMessage(string message, string logLevel)
        {
            Console.WriteLine(DateTime.Now + " [" + logLevel + "] " + message);
        }

        static void Main(string[] args)
        {
            Process[] prcKpr = Process.GetProcessesByName("ProcessKeeper");
            foreach (var process in prcKpr)
            {
                process.Kill();
            }
            prcKpr = Process.GetProcessesByName("WorkNode");
            foreach (var process in prcKpr)
            {
                process.Kill();
            }
            LoadConfig();
            foreach (var server in ServerList)
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
                    using (var web = new WebClient())
                    {
                        try
                        {
                            FtpWebRequest reqFTP;
                            reqFTP = (FtpWebRequest)WebRequest.Create(new Uri("ftp://" + server.Host + ":" + server.Port + server.LogsDir + fileList[i]));
                            reqFTP.Credentials = new NetworkCredential(server.UserName, server.UserPassword);
                            reqFTP.KeepAlive = false;
                            reqFTP.Method = WebRequestMethods.Ftp.DeleteFile;
                            string result = String.Empty;
                            FtpWebResponse _response = (FtpWebResponse)reqFTP.GetResponse();
                            long size = _response.ContentLength;
                            Stream datastream = _response.GetResponseStream();
                            StreamReader sr = new StreamReader(datastream);
                            result = sr.ReadToEnd();
                            sr.Close();
                            datastream.Close();
                            _response.Close();
                            PrintLogMessage("[" + server.UserName + ":" + server.Host + "] deleted file " + fileList[i], "INFO");
                        }
                        catch (Exception exp)
                        {
                            PrintLogMessage(exp.Message + " " + fileList[i], "ERROR");
                        }
                    }
                }
                reader.Close();
                responseStream.Close();
                response.Close();
            }
            Process.Start(@"C:\kTVCSS\_process\ProcessKeeper.exe");
        }
    }
}
