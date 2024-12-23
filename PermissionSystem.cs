using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace TeamXServer
{
    public class PermissionPlayer
    {
        public string Name { get; set; }
        public string PermissionLevel { get; set; }
    }

    public class PermissionEntry
    {
        public bool IsAdministrator { get; set; }
        public bool CanJoin { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanEditAll { get; set; }
        public bool CanEditFloor { get; set; }
        public bool CanEditSkybox { get; set; }
        public bool CanDestroy { get; set; }
        public int BlockLimit { get; set; }
        public List<int> BannedBlocks = new List<int>();
    }

    public class PermissionConfig
    {
        public Dictionary<string, PermissionPlayer> Players { get; set; } = new Dictionary<string, PermissionPlayer>();
        public Dictionary<string, PermissionEntry> Permissions { get; set; } = new Dictionary<string, PermissionEntry>();
    }

    public class PermissionSystem
    {
        public PermissionConfig CurrentConfig { get; private set; }
        private readonly string _filePath;

        public PermissionSystem()
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "permissions.json");
            LoadPermissionsFromFile();
        }

        public (ulong,string,string) GetPermissionEntry(ulong steamID)
        {
            string steamIDString = steamID.ToString();

            if(!CurrentConfig.Players.ContainsKey(steamIDString))
            {
                return (steamID, "", "default");
            }

            PermissionPlayer player = CurrentConfig.Players[steamIDString];
            return (steamID, player.Name, player.PermissionLevel);
        }

        public PermissionEntry GetPermissions(ulong steamID)
        {
            string steamIDString = steamID.ToString();

            if (CurrentConfig == null)
            {
                return GetDefaultPermissions();
            }

            if (!CurrentConfig.Players.ContainsKey(steamIDString))
            {
                // Add the unknown player with default permissions
                AddPlayer(steamIDString, "default");
                SavePermissionsToFile();
            }

            string permissionLevel = CurrentConfig.Players[steamIDString].PermissionLevel;
            return CurrentConfig.Permissions.ContainsKey(permissionLevel)
                ? CurrentConfig.Permissions[permissionLevel]
                : GetDefaultPermissions();
        }

        public void AddPlayer(string steamID, string permissionLevel)
        {
            if (!CurrentConfig.Players.ContainsKey(steamID))
            {
                CurrentConfig.Players[steamID] = new PermissionPlayer
                {
                    Name = $"Player_{steamID}",
                    PermissionLevel = permissionLevel
                };
            }
        }

        public void UpdatePlayerPermission(string steamID, string newPermissionLevel)
        {
            if (CurrentConfig.Players.ContainsKey(steamID))
            {
                CurrentConfig.Players[steamID].PermissionLevel = newPermissionLevel;
                SavePermissionsToFile();
            }
            else
            {
                AddPlayer(steamID, newPermissionLevel);
                SavePermissionsToFile();
            }
        }

        public void UpdatePlayerName(string steamID, string newName)
        {
            if (CurrentConfig.Players.ContainsKey(steamID))
            {
                CurrentConfig.Players[steamID].Name = newName;
                SavePermissionsToFile();
            }
        }

        public void LoadPermissionsFromFile()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string jsonContent = File.ReadAllText(_filePath);
                    CurrentConfig = JsonConvert.DeserializeObject<PermissionConfig>(jsonContent);
                    Logger.Log("Loaded permissions!", LogType.Message);
                }
                else
                {
                    // Initialize with a default configuration if file does not exist
                    CurrentConfig = new PermissionConfig();
                    SavePermissionsToFile();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading or parsing the JSON file: {ex.Message}");
                CurrentConfig = new PermissionConfig();
            }
        }

        public void SavePermissionsToFile()
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(CurrentConfig, Formatting.Indented);
                File.WriteAllText(_filePath, jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to the JSON file: {ex.Message}");
            }
        }

        private PermissionEntry GetDefaultPermissions()
        {
            return new PermissionEntry
            {
                IsAdministrator = false,
                CanJoin = true,
                CanCreate = true,
                CanEdit = true,
                CanEditAll = false,
                CanDestroy = true,
                BannedBlocks = new List<int>(),
                BlockLimit = 200
            };
        }

        public string PermissionToDebugString(PermissionEntry permissions)
        {
            if (permissions == null)
            {
                return "Permissions object is null.";
            }

            return $@"
IsAdministrator: {permissions.IsAdministrator}
CanJoin: {permissions.CanJoin}
CanCreate: {permissions.CanCreate}
CanEdit: {permissions.CanEdit}
CanEditAll: {permissions.CanEditAll}
CanEditFloor: {permissions.CanEditFloor}
CanEditSkybox: {permissions.CanEditSkybox}
CanDestroy: {permissions.CanDestroy}
BlockLimit: {permissions.BlockLimit}
BannedBlocks: [{string.Join(", ", permissions.BannedBlocks ?? new List<int>())}]";
        }
    }
}
