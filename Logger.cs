using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamXServer
{
    public enum LogType { Debug = 0, Message = 1, Warning = 2, Error = 3 }

    public static class Logger
    {
        public static int logLevel = 1;

        public static void Log(string message, LogType logType, bool header = true)
        {
            var previousColor = Console.ForegroundColor;

            switch (logType)
            {
                case LogType.Debug:
                    if(logLevel <= 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine($" {(header ? "[TEAMX]" : "")} {message}");
                    }
                    break;
                case LogType.Message:
                    if (logLevel <= 1)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($" {(header ? "[TEAMX]" : "")} {message}");
                    }
                    break;
                case LogType.Warning:
                    if (logLevel <= 2)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($" {(header ? "[TEAMX]" : "")} {message}");
                    }
                    break;
                case LogType.Error:
                    if (logLevel <= 3)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($" {(header ? "[TEAMX]" : "")} {message}");
                    }
                    break;
            }

            Console.ForegroundColor = previousColor;
        }
    }
}
