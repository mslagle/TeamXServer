using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TeamXServer
{
    public class Editor
    {
        public int Floor { get; private set; }
        public string Skybox { get; private set; }
        public Dictionary<string, BlockPropertyJSONX> Blocks { get; private set; }
        public Dictionary<string, ulong> Selections { get; private set; }

        public Editor()
        {
            Floor = 90;
            Skybox = "{\"enviro\":{\"skybox\":0,\"groundMat\":90,\"overrideFog_b\":false,\"overrideFog_f\":0,\"skyboxOverride\":null}}";
            Blocks = new Dictionary<string, BlockPropertyJSONX>();
            Selections = new Dictionary<string, ulong>();
        }

        public void Clear()
        {
            Floor = 90;
            Skybox = "{\"enviro\":{\"skybox\":0,\"groundMat\":90,\"overrideFog_b\":false,\"overrideFog_f\":0,\"skyboxOverride\":null}}";
            Blocks = new Dictionary<string, BlockPropertyJSONX>();
            Selections = new Dictionary<string, ulong>();
        }

        public void ClearAllSelections()
        {
            Selections = new Dictionary<string, ulong>();
        }

        public bool IsSelected(string UID)
        {
            return Selections.ContainsKey(UID);
        }

        public ulong SelectedByWho(string UID)
        {
            if (Selections.ContainsKey(UID))
            {
                return Selections[UID];
            }

            return 0;
        }

        public bool IsSelectedBy(string UID, ulong steamID)
        {
            if(Selections.ContainsKey(UID))
            {
                if (Selections[UID] == steamID)
                {
                    return true;
                }
            }

            return false;
        }

        public List<ulong> GetAllSteamIDs()
        {
            List<ulong> steamIDs = new List<ulong>();
            foreach(BlockPropertyJSONX block in Blocks.Values)
            {
                if(!steamIDs.Contains(block.SteamID))
                {
                    steamIDs.Add(block.SteamID);
                }
            }
            return steamIDs;
        }

        public void RemoveAllSelectionsFrom(ulong steamID)
        {
            List<string> keysToRemove = new List<string>();

            // Find matching keys
            foreach (var kvp in Selections)
            {
                if (kvp.Value == steamID)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            // Remove matching keys
            foreach (var key in keysToRemove)
            {
                Selections.Remove(key);
            }
        }

        public BlockPropertyJSONX GetBlock(string UID)
        {
            if(Blocks.ContainsKey(UID))
            {
                return Blocks[UID];
            }

            return null;
        }

        public void Select(string UID, ulong steamID)
        {
            if (Blocks.ContainsKey(UID))
            {
                if (!Selections.ContainsKey(UID))
                {
                    Selections.Add(UID, steamID);
                }
            }
        }

        public void Deselect(string UID)
        {
            if (Selections.ContainsKey(UID))
            {
                Selections.Remove(UID);
            }
        }

        public void Add(BlockPropertyJSONX block)
        {
            if (!Blocks.ContainsKey(block.blockPropertyJSON.u))
            {
                Blocks.Add(block.blockPropertyJSON.u, block);
            }
            else
            {
                Logger.Log("Can't add block because UID already exists. UID: " + block.blockPropertyJSON.u, LogType.Warning);
            }
        }

        public void Add(string blockString)
        {
            BlockPropertyJSONX block = BlockPropertyJSONX.FromJson(blockString);
            if (!Blocks.ContainsKey(block.blockPropertyJSON.u))
            {
                Blocks.Add(block.blockPropertyJSON.u, block);
            }
            else
            {
                Logger.Log("Can't add block because UID already exists. UID: " + block.blockPropertyJSON.u, LogType.Warning);
            }
        }

        public void Remove(string uid)
        {
            if (Blocks.ContainsKey(uid))
            {
                Blocks.Remove(uid);

                if (Selections.ContainsKey(uid))
                {
                    Selections.Remove(uid);
                }
            }
            else
            {
                Logger.Log("Can't remove block because UID doesn't exist. UID: " + uid, LogType.Warning);
            }
        }

        public void Update(BlockPropertyJSONX block)
        {
            if (Blocks.ContainsKey(block.blockPropertyJSON.u))
            {
                Blocks[block.blockPropertyJSON.u] = block;
            }
            else
            {
                Logger.Log("Can't update block because UID doesn't exist. UID: " + block.blockPropertyJSON.u, LogType.Warning);
            }
        }

        public void SetFloor(int floor)
        {
            Floor = floor;
        }

        public void SetSkybox(string skybox)
        {
            Skybox = skybox;
        }

        public void ImportSaveFile(SaveFile saveFile)
        {
            // Set floor and skybox IDs
            Floor = saveFile.Floor;
            Skybox = saveFile.Skybox;

            // Clear current blocks and load from the save
            Blocks.Clear();

            foreach (BlockPropertyJSONX block in saveFile.Blocks)
            {
                try
                {
                    Blocks.Add(block.blockPropertyJSON.u, block);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error loading block UID {block.blockPropertyJSON.u}: {ex.Message}", LogType.Warning);
                }
            }
        }

        public SaveFile CreateSaveFile()
        {
            return new SaveFile
            {
                Floor = Floor,
                Skybox = Skybox,
                Blocks = Blocks.Values.ToList()
            };
        }

        public List<string> GetBlockStrings()
        {
            List<string> blockStrings = new List<string>();
            foreach(BlockPropertyJSONX block in Blocks.Values)
            {
                blockStrings.Add(block.ToJson());
            }
            return blockStrings;
        }

        public string GetBlockString(string UID)
        {
            if(Blocks.ContainsKey(UID))
            {
                return Blocks[UID].ToJson();
            }

            return "";
        }
    }
}
