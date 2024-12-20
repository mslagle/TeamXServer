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
    public class SaveManager
    {
        private DateTime lastSaveTime;

        public SaveManager()
        {
            InitializeDirectories();

            if(Program.Config.LoadBackupOnStart)
            {
                TryLoadLatestSave();
            }
        }

        private void InitializeDirectories()
        {
            EnsureDirectoryExists(Program.Config.ProjectPath);
            EnsureDirectoryExists(Program.Config.ZeepSavePath);
            EnsureDirectoryExists(Program.Config.ServerSavePath);
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
            var saves = GetSaveFiles(Program.Config.ServerSavePath, "*.teamkist");

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

            var saves = GetSaveFiles(Program.Config.ServerSavePath, "*.teamkist");

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

        private void LoadSave(FileInfo saveFile)
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
                CleanupOldFiles(Program.Config.ServerSavePath, "*.teamkist");
                CleanupOldFiles(Program.Config.ZeepSavePath, "*.zeeplevel");

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
            string filePath = GetTimestampedFilePath(Program.Config.ServerSavePath, $"{Program.Config.LevelName}.teamkist");
            File.WriteAllText(filePath, JsonConvert.SerializeObject(saveFile));
            Logger.Log($"Server save created: {filePath}", LogType.Message);
        }

        private void SaveZeepFile(SaveFile saveFile)
        {
            string filePath = GetTimestampedFilePath(Program.Config.ZeepSavePath, $"{Program.Config.LevelName}.zeeplevel");
            File.WriteAllLines(filePath, FormatZeepFile(saveFile));
            Logger.Log($"Zeep save created: {filePath}", LogType.Message);
        }

        private List<string> FormatZeepFile(SaveFile saveFile)
        {
            var lines = new List<string>();
            string uid = GenerateUID(saveFile);
            lines.Add($"LevelEditor2,{Program.Config.LevelName},{uid}");
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
            string parsedName = Regex.Replace(Program.Config.LevelName, @"[^a-zA-Z0-9\s]", "");
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string randomPart = new Random().Next(100000, 999999).ToString();
            return $"{timestamp}-{parsedName}-{randomPart}-{saveFile.Blocks.Count}";
        }

        private void CleanupOldFiles(string directory, string searchPattern)
        {
            var files = GetSaveFiles(directory, searchPattern);
            if (files.Length > Program.Config.BackupCount)
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
            if ((DateTime.Now - lastSaveTime).TotalSeconds >= Program.Config.AutoSaveInterval)
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
