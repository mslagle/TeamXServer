using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace TKServerConsole
{
    /*
    public static class TKEditor
    {
        public static int floorID;
        public static int skyboxID;
        public static Dictionary<string, TKBlock> blocks;

        public static void Initialize()
        {
            floorID = 90;
            skyboxID = 0;
            blocks = new Dictionary<string, TKBlock>();
        }

        public static NetOutgoingMessage GenerateServerDataMessage()
        {
            NetOutgoingMessage outgoingMessage = TKServer.server.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.ServerData);
            outgoingMessage.Write(floorID);
            outgoingMessage.Write(skyboxID);
            outgoingMessage.Write(blocks.Count);
            foreach (KeyValuePair<string, TKBlock> tkblock in blocks)
            {
                outgoingMessage.Write(TKUtilities.GetJSONString(tkblock.Value));
            }
            return outgoingMessage;
        }

        public static void BlockCreated(string blockJSON)
        {
            TKBlock tkBlock = TKUtilities.JSONToTKBlock(blockJSON);
            if (!blocks.ContainsKey(tkBlock.UID))
            {
                blocks.Add(tkBlock.UID, tkBlock);
            }
            else
            {
                Program.Log("Can't add block because UID already exists. UID: " + tkBlock.UID);
            }
        }

        public static void BlockDestroyed(string UID)
        {
            if (blocks.ContainsKey(UID))
            {
                blocks.Remove(UID);
            }
            else
            {
                Program.Log("Can't remove block because UID doesn't exist. UID: " + UID);
            }
        }

        public static void BlockUpdated(string UID, string properties)
        {
            if (blocks.ContainsKey(UID))
            {
                TKUtilities.AssignPropertiesToTKBlock(blocks[UID], properties);                
            }
            else
            {
                Program.Log("Can't update block because UID doesn't exist. UID: " + UID);
            }
        }

        public static void FloorUpdated(int floor)
        {
            floorID = floor;
        }

        public static void SkyboxUpdated(int skybox)
        {
            skyboxID = skybox;
        }
    }*/
}
