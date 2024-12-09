using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TKServerConsole;

namespace TeamXServer
{
    public class Editor
    {
        public int Floor { get; private set; }
        public int Skybox { get; private set; }
        public Dictionary<string, Block> Blocks { get; private set; }
        public Dictionary<string, ulong> Selections { get; private set; }

        public Editor()
        {
            Floor = 90;
            Skybox = 0;
            Blocks = new Dictionary<string, Block>();
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

        public Block GetBlock(string UID)
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

        public void Add(Block block)
        {
            if (!Blocks.ContainsKey(block.UID))
            {
                Blocks.Add(block.UID, block);
            }
            else
            {
                Logger.Log("Can't add block because UID already exists. UID: " + block.UID, LogType.Warning);
            }
        }

        public void Add(string blockString)
        {
            Block block = JSONToBlock(blockString);
            if (!Blocks.ContainsKey(block.UID))
            {
                Blocks.Add(block.UID, block);
            }
            else
            {
                Logger.Log("Can't add block because UID already exists. UID: " + block.UID, LogType.Warning);
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

        public void Update(Block block)
        {
            if (Blocks.ContainsKey(block.UID))
            {
                Blocks[block.UID] = block;
            }
            else
            {
                Logger.Log("Can't update block because UID doesn't exist. UID: " + block.UID, LogType.Warning);
            }
        }

        public void SetFloor(int floor)
        {
            Floor = floor;
        }

        public void SetSkybox(int skybox)
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

            foreach (Block block in saveFile.Blocks)
            {
                try
                {
                    Blocks.Add(block.UID, block);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error loading block UID {block.UID}: {ex.Message}", LogType.Warning);
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
            foreach(Block block in Blocks.Values)
            {
                blockStrings.Add(BlockToJSON(block));
            }
            return blockStrings;
        }

        public string GetBlockString(string UID)
        {
            if(Blocks.ContainsKey(UID))
            {
                return BlockToJSON(Blocks[UID]);
            }

            return "";
        }

        public Block JSONToBlock(string json)
        {
            Block block = JsonConvert.DeserializeObject<Block>(json);
            return block;
        }

        public string BlockToJSON(Block block)
        {
            return JsonConvert.SerializeObject(block);
        }

        private void SetBlockProperties(Block block, string properties)
        {
            List<float> propertyList = PropertyStringToList(properties);
            block.PositionX = propertyList[0];
            block.PositionY = propertyList[1];
            block.PositionZ = propertyList[2];
            block.EulerAnglesX = propertyList[3];
            block.EulerAnglesY = propertyList[4];
            block.EulerAnglesZ = propertyList[5];
            block.LocalScaleX = propertyList[6];
            block.LocalScaleY = propertyList[7];
            block.LocalScaleZ = propertyList[8];
            block.Properties = propertyList;
        }

        private List<float> PropertyStringToList(string properties)
        {
            return properties.Split('|').Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToList();
        }
    }
}
