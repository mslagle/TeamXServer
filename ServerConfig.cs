using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TeamXServer
{
    public class ServerConfig
    {
        public IPAddress ServerIP { get; private set; }
        public int ServerPort { get; private set; }
        public string LevelName { get; private set; }
        public int AutoSaveInterval { get; private set; }
        public int BackupCount { get; private set; }
        public bool LoadBackupOnStart { get; private set; }
        public bool KeepBackupWithNoEditors { get; private set; }
        public string ServerBasePath { get; private set; }
        public string ProjectPath { get; private set; }
        public string ZeepSavePath { get; private set; }
        public string ServerSavePath { get; private set; }

        private readonly IPAddress DEFAULT_IP = IPAddress.Parse("0.0.0.0");
        private const int DEFAULT_PORT = 8080;
        private const string DEFAULT_LEVEL_NAME = "TeamKist";
        private const int DEFAULT_AUTO_SAVE_INTERVAL = 300;
        private const int DEFAULT_BACKUP_COUNT = 10;
        private const bool DEFAULT_LOAD_BACKUP_ON_START = true;
        private const bool DEFAULT_KEEP_BACKUP_WITH_NO_EDITORS = true;

        public void Load()
        {
            var serverIpString = ConfigurationManager.AppSettings["ServerIP"];
            var serverPortString = ConfigurationManager.AppSettings["ServerPort"];
            var levelName = ConfigurationManager.AppSettings["LevelName"];
            var autoSaveIntervalString = ConfigurationManager.AppSettings["AutoSaveInterval"];
            var backupCountString = ConfigurationManager.AppSettings["BackupCount"];
            var loadBackupOnStart = ConfigurationManager.AppSettings["LoadBackupOnStart"];
            var keepBackupWithNoEditors = ConfigurationManager.AppSettings["KeepBackupWithNoEditors"];

            ServerIP = string.IsNullOrWhiteSpace(serverIpString) ? DEFAULT_IP : IPAddress.Parse(serverIpString);
            ServerPort = string.IsNullOrWhiteSpace(serverPortString) ? DEFAULT_PORT : int.Parse(serverPortString);
            AutoSaveInterval = string.IsNullOrWhiteSpace(autoSaveIntervalString) ? DEFAULT_AUTO_SAVE_INTERVAL : int.Parse(autoSaveIntervalString);
            BackupCount = string.IsNullOrWhiteSpace(backupCountString) ? DEFAULT_BACKUP_COUNT : int.Parse(backupCountString);
            LoadBackupOnStart = bool.TryParse(loadBackupOnStart, out var loadBackup) ? loadBackup : DEFAULT_LOAD_BACKUP_ON_START;
            KeepBackupWithNoEditors = bool.TryParse(keepBackupWithNoEditors, out var keepBackup) ? keepBackup : DEFAULT_KEEP_BACKUP_WITH_NO_EDITORS;

            levelName = Path.GetInvalidFileNameChars().Aggregate(levelName ?? string.Empty, (current, c) => current.Replace(c, '_')).Replace(".zeeplevel", "");
            LevelName = string.IsNullOrWhiteSpace(levelName) ? DEFAULT_LEVEL_NAME : levelName;

            ServerBasePath = AppDomain.CurrentDomain.BaseDirectory;
            ProjectPath = Path.Combine(ServerBasePath, levelName);
            ZeepSavePath = Path.Combine(ProjectPath, "ZeepSaves");
            ServerSavePath = Path.Combine(ProjectPath, "ServerSaves");
        }
    }
}
