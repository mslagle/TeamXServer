﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TeamXServer
{
    public class SaveJSON
    {
        public string LevelName { get; set; }
        public int AutoSaveInterval { get; set; }
        public int BackupCount { get; set; }
        public bool LoadBackupOnStart { get; set; }
        public bool KeepBackupWithNoEditors { get; set; }
    }

    public class SaveManager
    {
        private readonly string _filePath;
        public SaveJSON saveConfiguration;
        private DateTime lastSaveTime;

        public string ServerBasePath { get; set; }
        public string ProjectPath;
        public string ServerSavePath;

        public SaveManager()
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "userdata", "save.json");
            saveConfiguration = ReadConfig();
            if(saveConfiguration == null)
            {
                SaveJSON saveJSON = new SaveJSON()
                {
                    LevelName = "TeamXServer",
                    AutoSaveInterval = 60,
                    BackupCount = 5,
                    LoadBackupOnStart = true,
                    KeepBackupWithNoEditors = true
                };

                saveConfiguration = saveJSON;
                WriteConfig(saveJSON);
            }

            InitializeDirectories();

            if(saveConfiguration.LoadBackupOnStart)
            {
                TryLoadLatestSave();
            }
        }

        public void ApplySaveConfiguration(SaveJSON saveJSON)
        {
            saveConfiguration = saveJSON;
            WriteConfig(saveJSON);
            InitializeDirectories();
        }

        private void InitializeDirectories()
        {
            ServerBasePath = AppDomain.CurrentDomain.BaseDirectory;
            ProjectPath = Path.Combine(ServerBasePath, "userdata", saveConfiguration.LevelName);
            ServerSavePath = Path.Combine(ProjectPath, "ServerSaves");

            EnsureDirectoryExists(ProjectPath);
            EnsureDirectoryExists(ServerSavePath);
        }

        /// <summary>
        /// Reads the SaveJSON configuration from the specified file path.
        /// </summary>
        /// <returns>A SaveJSON object populated with data from the file.</returns>
        public SaveJSON ReadConfig()
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            string jsonContent = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<SaveJSON>(jsonContent);
        }

        /// <summary>
        /// Writes the updated SaveJSON configuration to the same file path.
        /// </summary>
        /// <param name="config">The updated SaveJSON object to write.</param>
        public void WriteConfig(SaveJSON config)
        {
            string jsonContent = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(_filePath, jsonContent);
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Logger.Log($"Created directory: {path}", LogType.Message);
            }
        }

        public void TryLoadLatestSave()
        {
            var saves = GetSaveFiles(ServerSavePath, "*.teamkist");

            if (saves.Length == 0)
            {
                Logger.Log("No saves found.", LogType.Message);
                return;
            }

            Logger.Log("Loading latest save...", LogType.Message);
            LoadSave(saves.OrderByDescending(f => f.CreationTime).First());
            lastSaveTime = DateTime.Now;
        }    

        public void LoadSave(FileInfo saveFile)
        {
            try
            {
                Logger.Log($"Loading save file: {saveFile.Name}", LogType.Message);

                // Read the file content
                string jsonString = File.ReadAllText(saveFile.FullName);

                // Deserialize the JSON into a TKSaveFile object
                SaveFile loadedSave = JsonConvert.DeserializeObject<SaveFile>(jsonString);

                if (loadedSave == null)
                {
                    throw new InvalidDataException("Save file content could not be deserialized.");
                }

                // Apply the loaded data to the editor
                Program.editor.ImportSaveFile(loadedSave);

                Logger.Log("Save file loaded successfully!", LogType.Message);
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading save file: {ex.Message}", LogType.Error);
            }
        }        

        public void Save()
        {
            try
            {
                bool shouldSave = Program.playerManager.connectedPlayers.Count > 0 || saveConfiguration.KeepBackupWithNoEditors;

                if (shouldSave)
                {
                    Logger.Log("Saving...", LogType.Message);

                    var saveFile = Program.editor.CreateSaveFile();
                    SaveServerFile(saveFile);
                    CleanupOldFiles(ServerSavePath, "*.teamkist");
                    Logger.Log("Save completed successfully!", LogType.Message);
                }
                else
                {
                    Logger.Log("Save will be skipped.", LogType.Message);
                }

                lastSaveTime = DateTime.Now;                
            }
            catch (Exception ex)
            {
                Logger.Log($"Error during save: {ex.Message}", LogType.Error);
            }
        }        

        private void SaveServerFile(SaveFile saveFile)
        {
            string filePath = GetTimestampedFilePath(ServerSavePath, $"{saveConfiguration.LevelName}.teamkist");
            File.WriteAllText(filePath, JsonConvert.SerializeObject(saveFile));
            Logger.Log($"Server save created: {filePath}", LogType.Message);
        }

        private void CleanupOldFiles(string directory, string searchPattern)
        {
            var files = GetSaveFiles(directory, searchPattern);
            if (files.Length > saveConfiguration.BackupCount)
            {
                var fileToDelete = files.OrderBy(f => f.CreationTime).First();
                File.Delete(fileToDelete.FullName);
                Logger.Log($"Deleted old save: {fileToDelete.Name}", LogType.Message);
            }
        }

        private FileInfo[] GetSaveFiles(string directory, string searchPattern)
        {
            return new DirectoryInfo(directory).GetFiles(searchPattern);
        }

        public void RunAutoSave()
        {
            if ((DateTime.Now - lastSaveTime).TotalSeconds >= saveConfiguration.AutoSaveInterval)
            {
                Save();
            }
        }

        private string GetTimestampedFilePath(string directoryPath, string fileName)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string extension = Path.GetExtension(fileName);
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            return Path.Combine(directoryPath, $"{baseName}_{timestamp}{extension}");
        }
    }
}
