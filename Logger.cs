using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamXServer
{
    public static class Logger
    {
        public static void Log(string message, bool header = true)
        {
            Console.WriteLine($" {(header?"[TEAMX]" : "")} {message}");
        }

        public static void LogColored(string message, ConsoleColor color, bool header = true)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Log(message, header);
            Console.ForegroundColor = previousColor;
        }
    }
}
