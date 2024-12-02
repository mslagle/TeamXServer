using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamXServer
{
    public static class Graphics
    {
        public static string[] logo = new string[]
        {
            @" ████████╗███████╗ █████╗ ███╗   ███╗██╗  ██╗",
            @" ╚══██╔══╝██╔════╝██╔══██╗████╗ ████║╚██╗██╔╝",
            @"    ██║   █████╗  ███████║██╔████╔██║ ╚███╔╝ ",
            @"    ██║   ██╔══╝  ██╔══██║██║╚██╔╝██║ ██╔██╗ ",
            @"    ██║   ███████╗██║  ██║██║ ╚═╝ ██║██╔╝ ██╗",
            @"    ╚═╝   ╚══════╝╚═╝  ╚═╝╚═╝     ╚═╝╚═╝  ╚═╝",
            @"                                  by Metalted"                                           
        };

        public static void ShowLogo()
        {
            Logger.Log("", false);
            Logger.LogColored(logo[0], ConsoleColor.White, false);
            Logger.LogColored(logo[1], ConsoleColor.White, false);
            Logger.LogColored(logo[2], ConsoleColor.White, false);
            Logger.LogColored(logo[3], ConsoleColor.White, false);
            Logger.LogColored(logo[4], ConsoleColor.White, false);
            Logger.LogColored(logo[5], ConsoleColor.White, false);
            Logger.LogColored(logo[6], ConsoleColor.White, false);
            Logger.Log("", false);
        }

        public static void LogConfiguration(ServerConfig config)
        {
            Logger.Log($"IP:\t\t\t{config.ServerIP}");
            Logger.Log($"Port:\t\t{config.ServerPort}");
            Logger.Log($"Level Name:\t\t{config.LevelName}");
            Logger.Log($"Auto Save Interval:\t{config.AutoSaveInterval}");
            Logger.Log($"Backup Count:\t{config.BackupCount}");
            Logger.Log($"Loading Backup:\t{config.LoadBackupOnStart}");
            Logger.Log($"Keeping Backup with No Editors:\t{config.KeepBackupWithNoEditors}");
        }
    }
}
