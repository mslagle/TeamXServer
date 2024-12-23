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
            Logger.Log("", LogType.Message, false);
            Logger.Log(logo[0], LogType.Message, false);
            Logger.Log(logo[1], LogType.Message, false);
            Logger.Log(logo[2], LogType.Message, false);
            Logger.Log(logo[3], LogType.Message, false);
            Logger.Log(logo[4], LogType.Message, false);
            Logger.Log(logo[5], LogType.Message, false);
            Logger.Log(logo[6], LogType.Message, false);
            Logger.Log("", LogType.Message, false);
        }

        /*
        public static void LogConfiguration(ServerConfig config)
        {
            Logger.Log($"IP:\t\t\t{config.ServerIP}", LogType.Message);
            Logger.Log($"Port:\t\t{config.ServerPort}", LogType.Message);
            Logger.Log($"Level Name:\t\t{config.LevelName}", LogType.Message);
            Logger.Log($"Auto Save Interval:\t{config.AutoSaveInterval}", LogType.Message);
            Logger.Log($"Backup Count:\t{config.BackupCount}", LogType.Message);
            Logger.Log($"Loading Backup:\t{config.LoadBackupOnStart}", LogType.Message);
            Logger.Log($"Keeping Backup with No Editors:\t{config.KeepBackupWithNoEditors}", LogType.Message);
        }*/

        public static void LogConfiguration(ServerJSON config)
        {
            Logger.Log($"IP:\t\t\t{config.ServerIP}", LogType.Message);
            Logger.Log($"Port:\t\t{config.ServerPort}", LogType.Message);
        }

        public static void LogConfiguration(SaveJSON config)
        {
            Logger.Log($"Level Name:\t\t{config.LevelName}", LogType.Message);
            Logger.Log($"Auto Save Interval:\t{config.AutoSaveInterval}", LogType.Message);
            Logger.Log($"Backup Count:\t{config.BackupCount}", LogType.Message);
            Logger.Log($"Loading Backup:\t{config.LoadBackupOnStart}", LogType.Message);
            Logger.Log($"Keeping Backup with No Editors:\t{config.KeepBackupWithNoEditors}", LogType.Message);
        }
    }
}
