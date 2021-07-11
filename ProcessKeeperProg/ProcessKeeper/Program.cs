using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessKeeper
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "ProcessKeeper";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("sleep for 3 secs for init app");
            Thread.Sleep(3000);
            Console.Clear();
            while (true)
            {               
                var workNode = Process.GetProcessesByName("WorkNode");
                if (workNode.Count() == 0)
                {
                    FileInfo fileInfo = new FileInfo(Properties.Settings1.Default.WorkNode);
                    Console.WriteLine(fileInfo.Directory.FullName);
                    ProcessStartInfo processStartInfo = new ProcessStartInfo()
                    {
                        WorkingDirectory = fileInfo.Directory.FullName,
                        FileName = Properties.Settings1.Default.WorkNode
                    };
                    Process.Start(processStartInfo);
                    Console.WriteLine(DateTime.Now + ": started process " + "WorkNode");
                }

                var kBot = Process.GetProcessesByName("kBot");
                if (kBot.Count() == 0)
                {
                    FileInfo fileInfo = new FileInfo(Properties.Settings1.Default.kurwanatorVkBot);
                    Console.WriteLine(fileInfo.Directory.FullName);
                    ProcessStartInfo processStartInfo = new ProcessStartInfo()
                    {
                        WorkingDirectory = fileInfo.Directory.FullName,
                        FileName = Properties.Settings1.Default.kurwanatorVkBot
                    };
                    Process.Start(processStartInfo);
                    Console.WriteLine(DateTime.Now + ": started process " + "kurwanatorVkBot");
                }

                var ccw = Process.GetProcessesByName("CustomCupWorker");
                if (ccw.Count() == 0)
                {
                    FileInfo fileInfo = new FileInfo(Properties.Settings1.Default.WorkNode);
                    Console.WriteLine(fileInfo.Directory.FullName);
                    ProcessStartInfo processStartInfo = new ProcessStartInfo()
                    {
                        WorkingDirectory = @"C:\kTVCSS\_process\Custom Cup Worker",
                        FileName = @"C:\kTVCSS\_process\Custom Cup Worker\CustomCupWorker.exe"
                    };
                    Process.Start(processStartInfo);
                    Console.WriteLine(DateTime.Now + ": started process " + "CCW");
                }

                Thread.Sleep(3000);
            }
        }
    }
}
