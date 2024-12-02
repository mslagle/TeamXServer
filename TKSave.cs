using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Globalization;

namespace TKServerConsole
{/*
    public static class TKSave
    {
        private static string projectPath;
        private static string zeepSavePath;
        private static string serverSavePath;
        private static DateTime lastSaveTime;

        public static void Initialize(Boolean loadExistingAtStartup)
        {
            projectPath = Path.Combine(Program.SERVER_BASE_PATH, Program.SERVER_LEVEL_NAME);
            zeepSavePath = Path.Combine(projectPath, "ZeepSaves");
            serverSavePath = Path.Combine(projectPath, "ServerSaves");

            Program.Log($"Checking save directories for {Program.SERVER_LEVEL_NAME}.");
            if (!Directory.Exists(projectPath))
            {
                Directory.CreateDirectory(projectPath);
            }
            if (!Directory.Exists(zeepSavePath))
            {
                Directory.CreateDirectory(zeepSavePath);
            }
            if (!Directory.Exists(serverSavePath))
            {
                Directory.CreateDirectory(serverSavePath);
            }

            Program.Log("Searching for save files...");
            DirectoryInfo serverSavesDirectory = new DirectoryInfo(serverSavePath);
            FileInfo[] saves = serverSavesDirectory.GetFiles("*.teamkist", SearchOption.TopDirectoryOnly);

            if(saves.Length == 0)
            {
                Program.Log("No saves found.");
            }
            else if (loadExistingAtStartup == false)
            {
                Program.Log("LoadBackupOnStart is false, creating new save at startup.");
            }
            else
            {
                Program.Log("Loading latest save.");

                //Sort the saves them by creation date.
                saves = saves.OrderByDescending(f => f.CreationTime).ToArray();

                //Get the newest file.
                FileInfo newestSave = saves.FirstOrDefault();

                //Read the file 
                string jsonString = File.ReadAllText(newestSave.FullName);

                //Deserialize the object.
                TKSaveFile saveFile = JsonConvert.DeserializeObject<TKSaveFile>(jsonString);

                //Store it in the level editor.
                TKEditor.floorID = saveFile.floor;
                TKEditor.skyboxID = saveFile.skybox;

                TKEditor.blocks.Clear();

                foreach(TKBlock b in saveFile.blocks)
                {
                    TKEditor.blocks.Add(b.UID, b);
                }
            }

            lastSaveTime = DateTime.Now;
        }

        public static void Save()
        {
            Program.Log("Saving...");

            //Create a new save file
            TKSaveFile saveFile = new TKSaveFile();
            saveFile.floor = TKEditor.floorID;
            saveFile.skybox = TKEditor.skyboxID;
            saveFile.blocks = TKEditor.blocks.Values.ToList();

            //Create the json string.
            string jsonString = JsonConvert.SerializeObject(saveFile);

            //Create the file path for this new save.
            string filePath = GetTimestampedFilePath(serverSavePath, Program.SERVER_LEVEL_NAME + ".teamkist");

            //Write the file
            File.WriteAllText(filePath, jsonString);

            //Save the file in zeeplevel format.
            SaveZeeplevel(saveFile);

            //Delete save files if too many.
            ManageSaveFileCount();
            ManageZeepSaveFileCount();

            Program.Log("Saved!");
        }

        //Save a server file in zeeplevel format in the zeeplevel folder.
        public static void SaveZeeplevel(TKSaveFile saveFile)
        {
            //Ready to go! Create a 12 digit random number for the UID.
            Random random = new Random();
            string randomNumber = "";

            for (int i = 0; i < 12; i++)
            {
                randomNumber += random.Next(0, 10).ToString();
            }

            string parsedName = Regex.Replace(Program.SERVER_LEVEL_NAME, @"[^a-zA-Z0-9\s]", "");

            //Create the complete UID.
            DateTime now = DateTime.Now;
            string UID = now.Day.ToString("00") + now.Month.ToString("00") + now.Year.ToString() + "-" + now.Hour.ToString("00") + now.Minute.ToString("00") + now.Second.ToString("00") + now.Millisecond.ToString("000") + "-" + parsedName + "-" + randomNumber + "-" + saveFile.blocks.Count;

            //Create the list to hold the file.
            List<string> fileLines = new List<string>();

            //Create the header.
            fileLines.Add($"LevelEditor2,{parsedName},{UID}");
            fileLines.Add("0,0,0,0,0,0,0,0");
            fileLines.Add($"invalid track,0,0,0,{saveFile.skybox},{saveFile.floor}");

            foreach(TKBlock block in saveFile.blocks)
            {
                try
                {
                    fileLines.Add($"{block.blockID.ToString()},{string.Join(",", block.properties.Select(p => p.ToString(CultureInfo.InvariantCulture)))}");
                }
                catch
                {
                   
                }
            }

            string zeeppath = GetTimestampedFilePath(zeepSavePath, Program.SERVER_LEVEL_NAME + ".zeeplevel");

            //All lines are created, write the file.
            try
            {
                File.WriteAllLines(zeeppath, fileLines);
            }
            catch
            {
            }
        }

        //Make sure there are only as many save files as specified.
        public static void ManageSaveFileCount()
        {
            DirectoryInfo serverSaveDirectory = new DirectoryInfo(serverSavePath);
            FileInfo[] saves = serverSaveDirectory.GetFiles("*.teamkist", SearchOption.TopDirectoryOnly);
            if (saves.Length > Program.SERVER_BACKUP_COUNT)
            {
                saves = saves.OrderByDescending(f => f.CreationTime).ToArray();
                FileInfo last = saves.Last();

                if (File.Exists(last.FullName))
                {
                    File.Delete(last.FullName);
                }
            }
        }

        public static void ManageZeepSaveFileCount()
        {
            DirectoryInfo zeepSaveDirectory = new DirectoryInfo(zeepSavePath);
            FileInfo[] saves = zeepSaveDirectory.GetFiles("*.zeeplevel", SearchOption.TopDirectoryOnly);
            if (saves.Length > Program.SERVER_BACKUP_COUNT)
            {
                saves = saves.OrderByDescending(f => f.CreationTime).ToArray();
                FileInfo last = saves.Last();

                if (File.Exists(last.FullName))
                {
                    File.Delete(last.FullName);
                }
            }
        }

        public static void Run()
        {
            // Check if the amount of seconds has elapsed.
            if (DateTime.Now - lastSaveTime >= TimeSpan.FromSeconds(Program.SERVER_AUTO_SAVE_INTERVAL))
            {
                Save();
                lastSaveTime = DateTime.Now;
            }
        }

        public static string GetTimestampedFilePath(string directoryPath, string fileName)
        {
            // Create a timestamp string in the format "yyyyMMdd-HHmmss"
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

            // Get the file extension (if any) from the file name
            string extension = Path.GetExtension(fileName);

            // Create a new file name with the timestamp and extension (if any)
            string timestampedFileName = Path.GetFileNameWithoutExtension(fileName) + "_" + timestamp + extension;

            // Combine the directory path and timestamped file name to get the full file path
            string timestampedFilePath = Path.Combine(directoryPath, timestampedFileName);

            return timestampedFilePath;
        }
    }*/
}
