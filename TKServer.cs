using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace TKServerConsole
{
    /*
    public enum TKMessageType
    {
        LogIn = 10,
        JoinedPlayerData = 11,
        ServerPlayerData = 12,
        PlayerTransformData = 13,
        PlayerStateData = 14,
        PlayerLeft = 15,
        ServerData = 20,
        LevelEditorChangeEvents = 100,
        BlockCreateEvent = 101,
        BlockDestroyEvent = 102,
        BlockChangeEvent = 103,
        EditorFloorEvent = 104,
        EditorSkyboxEvent = 105,
        CustomMessage = 200
    }

    public static class TKServer
    {
        public static NetPeerConfiguration config;
        public static NetServer server;

        public static void Initialize()
        {
            config = new NetPeerConfiguration("Teamkist");
            config.Port = Program.SERVER_PORT;
            config.LocalAddress = IPAddress.Any;

            try
            {
                server = new NetServer(config);
                server.Start();
            }
            catch (Exception e)
            {
                Program.Log("A problem occured when setting up the server. Please check configuration.");
                Program.Log(e.ToString());
                throw;
            }

            Program.Log("Starting server on " + Program.SERVER_IP + ":" + Program.SERVER_PORT);
        }

        public static void Run()
        {
            NetIncomingMessage incomingMessage;
            while ((incomingMessage = server.ReadMessage()) != null)
            {
                //Get the connection of the player who send the message.
                NetConnection senderConnection = incomingMessage.SenderConnection;

                switch (incomingMessage.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                        switch (senderConnection.Status)
                        {
                            case NetConnectionStatus.Connected:
                                OnPlayerConnected(senderConnection);
                                break;
                            case NetConnectionStatus.Disconnected:
                                OnPlayerDisconnected(senderConnection);
                                break;
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        TKMessageType messageType = (TKMessageType)incomingMessage.ReadByte();

                        switch (messageType)
                        {
                            case TKMessageType.LogIn:

                                TKPlayer player = new TKPlayer()
                                {
                                    name = incomingMessage.ReadString(),
                                    zeepkist = incomingMessage.ReadInt32(),
                                    frontWheels = incomingMessage.ReadInt32(),
                                    rearWheels = incomingMessage.ReadInt32(),
                                    paraglider  = incomingMessage.ReadInt32(),
                                    horn = incomingMessage.ReadInt32(),
                                    hat =   incomingMessage.ReadInt32(),
                                    glasses = incomingMessage.ReadInt32(),
                                    color_body = incomingMessage.ReadInt32(),
                                    color_leftArm = incomingMessage.ReadInt32(),
                                    color_rightArm = incomingMessage.ReadInt32(),
                                    color_leftLeg = incomingMessage.ReadInt32(),
                                    color_rightLeg = incomingMessage.ReadInt32(),
                                    color = incomingMessage.ReadInt32(),
                                    connection = senderConnection,
                                    state = 0
                                };

                                TKPlayerManager.PlayerLogIn(player);

                                bool requestServerData = incomingMessage.ReadBoolean();
                                if (requestServerData)
                                {
                                    NetOutgoingMessage serverDataMessage = TKEditor.GenerateServerDataMessage();
                                    TKPlayerManager.SendMessageToSinglePlayer(serverDataMessage, player.connection);
                                }
                                break;
                            case TKMessageType.ServerData:
                                break;
                            case TKMessageType.PlayerTransformData:
                                Vector3 position = new Vector3();
                                position.x = incomingMessage.ReadFloat();
                                position.y = incomingMessage.ReadFloat();
                                position.z = incomingMessage.ReadFloat();
                                Vector3 euler = new Vector3();
                                euler.x = incomingMessage.ReadFloat();
                                euler.y = incomingMessage.ReadFloat();
                                euler.z = incomingMessage.ReadFloat();
                                byte pstate = incomingMessage.ReadByte();
                                TKPlayerManager.ProcessTransformDataMessage(senderConnection, position, euler, pstate);
                                break;
                            case TKMessageType.PlayerStateData:
                                byte state = incomingMessage.ReadByte();
                                TKPlayerManager.ProcessPlayerStateMessage(senderConnection, state);
                                break;
                            case TKMessageType.LevelEditorChangeEvents:

                                //Create a new message to send to other players so they receive the updates as well.
                                NetOutgoingMessage outgoingMessage = server.CreateMessage();
                                outgoingMessage.Write((byte)TKMessageType.LevelEditorChangeEvents);
                                
                                int changeCount = incomingMessage.ReadInt32();
                                outgoingMessage.Write(changeCount);

                                string blockJSON;
                                string UID;
                                string properties;
                                int floor;
                                int skybox;

                                for (int i = 0; i < changeCount; i++)
                                {
                                    TKMessageType changeEventType = (TKMessageType)incomingMessage.ReadByte();
                                    outgoingMessage.Write((byte)changeEventType);                                    
                                    
                                    switch (changeEventType)
                                    {
                                        case TKMessageType.BlockCreateEvent:
                                            blockJSON = incomingMessage.ReadString();
                                            TKEditor.BlockCreated(blockJSON);
                                            outgoingMessage.Write(blockJSON);
                                            break;
                                        case TKMessageType.BlockDestroyEvent:
                                            UID = incomingMessage.ReadString();
                                            TKEditor.BlockDestroyed(UID);
                                            outgoingMessage.Write(UID);
                                            break;
                                        case TKMessageType.BlockChangeEvent:
                                            UID = incomingMessage.ReadString();
                                            properties = incomingMessage.ReadString();
                                            TKEditor.BlockUpdated(UID, properties);
                                            outgoingMessage.Write(UID);
                                            outgoingMessage.Write(properties);
                                            break;
                                        case TKMessageType.EditorFloorEvent:
                                            floor = incomingMessage.ReadInt32();
                                            TKEditor.FloorUpdated(floor);
                                            outgoingMessage.Write(floor);
                                            break;
                                        case TKMessageType.EditorSkyboxEvent:
                                            skybox = incomingMessage.ReadInt32();
                                            TKEditor.SkyboxUpdated(skybox);
                                            outgoingMessage.Write(skybox);
                                            break;
                                    }
                                }

                                TKPlayerManager.SendMessageToAllPlayersExceptProvided(outgoingMessage, senderConnection);
                                break;
                            case TKMessageType.CustomMessage:
                                try
                                {
                                    string messagePayload = incomingMessage.ReadString();
                                    NetOutgoingMessage customOutgoingMessage = server.CreateMessage();

                                    //Get the id of the player
                                    int playerID = TKPlayerManager.players[senderConnection].ID;

                                    customOutgoingMessage.Write((byte)TKMessageType.CustomMessage);
                                    customOutgoingMessage.Write(messagePayload + ";" + playerID);
                                    TKPlayerManager.SendMessageToAllPlayersExceptProvided(customOutgoingMessage, senderConnection);
                                }
                                catch { }                     
                                break;
                        }
                        break;
                }
            }
        }

        public static void OnPlayerConnected(NetConnection senderConnection)
        {
            //Add the player to a dictionary or something.
        }

        public static void OnPlayerDisconnected(NetConnection senderConnection)
        {
            TKPlayerManager.PlayerLogOut(senderConnection);
        }
    }*/
}
