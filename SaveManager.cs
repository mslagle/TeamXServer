using Newtonsoft.Json;
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

        public string ServerBasePath;
        public string ProjectPath;
        public string ZeepSavePath;
        public string ServerSavePath;

        public SaveManager()
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "save.json");
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
            ProjectPath = Path.Combine(ServerBasePath, saveConfiguration.LevelName);
            ZeepSavePath = Path.Combine(ProjectPath, "ZeepSaves");
            ServerSavePath = Path.Combine(ProjectPath, "ServerSaves");

            EnsureDirectoryExists(ProjectPath);
            EnsureDirectoryExists(ZeepSavePath);
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

        public void Initialize(bool loadExistingAtStartup)
        {
            Logger.Log("Initializing saves...", LogType.Message);

            var saves = GetSaveFiles(ServerSavePath, "*.teamkist");

            if (saves.Length == 0)
            {
                Logger.Log("No saves found.", LogType.Message);
                return;
            }

            if (!loadExistingAtStartup)
            {
                Logger.Log("LoadBackupOnStart is false, creating new save at startup.", LogType.Message);
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
            Logger.Log("Saving...", LogType.Message);

            try
            {
                var saveFile = Program.editor.CreateSaveFile();
                SaveServerFile(saveFile);
                SaveZeepFile(saveFile);
                CleanupOldFiles(ServerSavePath, "*.teamkist");
                CleanupOldFiles(ZeepSavePath, "*.zeeplevel");

                lastSaveTime = DateTime.Now;
                Logger.Log("Save completed successfully!", LogType.Message);
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

        private void SaveZeepFile(SaveFile saveFile)
        {
            string filePath = GetTimestampedFilePath(ZeepSavePath, $"{saveConfiguration.LevelName}.zeeplevel");
            File.WriteAllLines(filePath, FormatZeepFile(saveFile));
            Logger.Log($"Zeep save created: {filePath}", LogType.Message);
        }

        private List<string> FormatZeepFile(SaveFile saveFile)
        {
            var lines = new List<string>();
            string uid = GenerateUID(saveFile);
            lines.Add($"LevelEditor2,{saveConfiguration.LevelName},{uid}");
            lines.Add("0,0,0,0,0,0,0,0");
            lines.Add($"invalid track,0,0,0,{saveFile.Skybox},{saveFile.Floor}");

            foreach (var block in saveFile.Blocks)
            {
                lines.Add($"{block.ID},{string.Join(",", block.Properties.Select(p => p.ToString(CultureInfo.InvariantCulture)))}");
            }

            return lines;
        }

        private string GenerateUID(SaveFile saveFile)
        {
            string parsedName = Regex.Replace(saveConfiguration.LevelName, @"[^a-zA-Z0-9\s]", "");
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string randomPart = new Random().Next(100000, 999999).ToString();
            return $"{timestamp}-{parsedName}-{randomPart}-{saveFile.Blocks.Count}";
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
