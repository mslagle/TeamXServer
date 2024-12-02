using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TKServerConsole;

namespace TeamXServer
{
    public class Server
    {
        private NetPeerConfiguration config;
        private NetServer server;

        public Server()
        {
            config = new NetPeerConfiguration("TeamX");
            config.Port = Program.Config.ServerPort;
            config.LocalAddress = IPAddress.Any;

            try
            {
                server = new NetServer(config);
                server.Start();
            }
            catch (Exception e)
            {
                Logger.Log("A problem occured when setting up the server. Please check configuration.");
                Logger.Log(e.ToString());
                throw;
            }

            Logger.Log("Starting server on " + Program.Config.ServerIP + ":" + Program.Config.ServerPort);
        }

        public void Run()
        {
            NetIncomingMessage im;
            while ((im = server.ReadMessage()) != null)
            {
                NetConnection senderConnection = im.SenderConnection;

                switch (im.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                        switch (senderConnection.Status)
                        {
                            case NetConnectionStatus.Connected:
                                //OnPlayerConnect(senderConnection);
                                break;
                            case NetConnectionStatus.Disconnected:
                                OnPlayerDisconnect(senderConnection);
                                break;
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        if (PacketUtility.Unpack(im, out ushort packetId))
                        {
                            Type packetType = PacketUtility.GetPacketType(packetId);

                            if (packetType != null)
                            {
                                var packet = (IPacket)Activator.CreateInstance(packetType);
                                packet.Deserialize(im);
                                Logger.Log($"Received packet of type: {packetType.Name}");
                                HandlePacket(packet, senderConnection);
                            }
                            else
                            {
                                Logger.Log($"Unknown packet ID: {packetId}");
                            }
                        }
                        else
                        {
                            Logger.Log("Failed to unpack the message.");
                        }
                        break;
                }
            }
        }

        public void HandlePacket(IPacket packet, NetConnection connection)
        {
            switch(packet)
            {
                case HandshakeResponsePacket handshakeResponsePacket:
                    HandleHandshakeResponse(handshakeResponsePacket, connection);
                    break;
                case PlayerJoinPacket playerJoinPacket:
                    HandlePlayerJoin(playerJoinPacket, connection);
                    break;
                case PlayerLeftPacket playerLeftPacket:
                    HandlePlayerLeft(playerLeftPacket, connection);
                    break;
                case EditorStateRequestPacket editorStateRequestPacket:
                    HandleEditorStateRequest(editorStateRequestPacket, connection);
                    break;
                case EditorBlockCreatePacket blockCreatePacket:
                    HandleEditorBlockCreate(blockCreatePacket, connection);
                    break;
                case EditorBlockUpdatePacket blockUpdatePacket:
                    HandleEditorBlockUpdate(blockUpdatePacket, connection);
                    break;
                case EditorBlockDestroyPacket blockDestroyPacket:
                    HandleEditorBlockDestroy(blockDestroyPacket, connection); 
                    break;
                case EditorFloorPacket floorPacket:
                    HandleEditorFloor(floorPacket, connection);
                    break;
                case EditorSkyboxPacket skyboxPacket:
                    HandleEditorSkybox(skyboxPacket, connection);
                    break;
                case EditorSelectionPacket editorSelectionPacket:
                    HandleEditorSelection (editorSelectionPacket, connection);
                    break;
                case EditorDeselectionPacket editorDeselectionPacket:
                    HandleEditorDeselection(editorDeselectionPacket, connection);
                    break;
                case PlayerStatePacket playerStatePacket:
                    HandlePlayerState(playerStatePacket, connection);
                    break;
            }
        }

        public bool Access(ulong steamID, NetConnection connection, bool sendAccessDenied = true)
        {
            PermissionLevel permissionLevel = Program.playerManager.GetPermissionLevel(steamID);
            if(permissionLevel == 0)
            {
                if (sendAccessDenied)
                {
                    AccessDeniedPacket accessDeniedPacket = new AccessDeniedPacket()
                    {
                        Reason = "Access denied. You are banned from this server."
                    };

                    var outgoingMessage = connection.Peer.CreateMessage();
                    PacketUtility.Pack(accessDeniedPacket, outgoingMessage);
                    connection.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 0);
                }
                return false;
            }
            return true;
        }

        public void OnPlayerConnect(NetConnection connection)
        {
            Logger.Log("Player connected. Sending handshake request...");

            var handshakeRequest = new HandshakeRequestPacket
            {
                Message = "Welcome to the server! Please introduce yourself."
            };

            var outgoingMessage = connection.Peer.CreateMessage();
            PacketUtility.Pack(handshakeRequest, outgoingMessage);
            connection.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public void OnPlayerDisconnect(NetConnection connection)
        {
            var player = Program.playerManager.GetPlayer(connection);
            if (player != null)
            {
                Program.playerManager.RemovePlayer(connection);
                Logger.Log($"Player {player.SteamID} disconnected.");
            }
        }

        public void HandleHandshakeResponse(HandshakeResponsePacket packet, NetConnection connection)
        {
            if(Access(packet.SteamID, connection))
            {
                AccessGrantedPacket accessGrantedPacket = new AccessGrantedPacket()
                {
                    Level = (byte)Program.playerManager.GetPermissionLevel(packet.SteamID),
                    Message = "Access Granted."
                };

                var outgoingMessage = connection.Peer.CreateMessage();
                PacketUtility.Pack(accessGrantedPacket, outgoingMessage);
                connection.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        public void HandlePlayerJoin(PlayerJoinPacket packet, NetConnection connection)
        {
            if(Access(packet.SteamID, connection))
            {
                Player player = Program.playerManager.AddPlayer(connection, packet.SteamID, Program.playerManager.GetPermissionLevel(packet.SteamID));
                player.SetProperties(packet);
                
                //Send the player joined packet to all other players
                Program.playerManager.SendToAllExcept(connection, packet);
                
                //Create a packet for each current player and send it back to the joiner.
                foreach(KeyValuePair<NetConnection, Player> p in Program.playerManager.connectedPlayers)
                {
                    if(p.Key == connection)
                    {
                        continue;
                    }

                    PlayerJoinPacket joinPacket = new PlayerJoinPacket()
                    {
                        Color = p.Value.Color,
                        Color_body = p.Value.Color_body,
                        Color_leftArm = p.Value.Color_leftArm,
                        Color_leftLeg = p.Value.Color_leftLeg,
                        Color_rightArm = p.Value.Color_rightArm,
                        Color_rightLeg = p.Value.Color_rightLeg,
                        FrontWheels = p.Value.FrontWheels,
                        Glasses = p.Value.Glasses,
                        Hat = p.Value.Hat,
                        Horn = p.Value.Horn,
                        Name = p.Value.Name,
                        Paraglider = p.Value.Paraglider,
                        RearWheels = p.Value.RearWheels,
                        SteamID = p.Value.SteamID,
                        Zeepkist = p.Value.Zeepkist
                    };

                    Program.playerManager.SendToPlayer(connection, joinPacket);
                }
            }
        }

        public void HandlePlayerLeft(PlayerLeftPacket packet, NetConnection connection)
        {
            if (Access(packet.SteamID, connection))
            {
                Program.playerManager.RemovePlayer(connection);
                Program.playerManager.SendToAllExcept(connection, packet);
            }
        }
        
        public void HandlePlayerState(PlayerStatePacket packet, NetConnection connection)
        {
            if(Access(packet.SteamID, connection))
            {
                Program.playerManager.UpdatePlayer(connection, packet);
                Program.playerManager.SendToAllExcept(connection, packet);
            }
        }
        
        public void HandleEditorStateRequest(EditorStateRequestPacket packet, NetConnection connection)
        {
            if (Access(packet.SteamID, connection))
            {
                EditorStateResponsePacket editorState = new EditorStateResponsePacket()
                {
                    BlockCount = Program.editor.Blocks.Count,
                    BlockStrings = Program.editor.GetBlockStrings(),
                    Floor = Program.editor.Floor,
                    Skybox = Program.editor.Skybox
                };

                var outgoingMessage = connection.Peer.CreateMessage();
                PacketUtility.Pack(editorState, outgoingMessage);
                connection.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        public void HandleEditorBlockCreate(EditorBlockCreatePacket packet, NetConnection connection)
        {
            Block packetBlock = Program.editor.JSONToBlock(packet.BlockString);

            if (Access(packet.SteamID, connection, false))
            {
                //Save the change
                Program.editor.Add(packetBlock);

                //Notify others
                Program.playerManager.SendToAllExcept(connection, packet);
            }
            else
            {
                EditorBlockCreateDeniedPacket editorBlockCreateDeniedPacket = new EditorBlockCreateDeniedPacket()
                {
                     UID = packetBlock.UID
                };

                Program.playerManager.SendToPlayer(connection, editorBlockCreateDeniedPacket);
            }
        }

        public void HandleEditorBlockUpdate(EditorBlockUpdatePacket packet, NetConnection connection)
        {
            Block packetBlock = Program.editor.JSONToBlock(packet.BlockString);

            if (Access(packet.SteamID, connection, false))
            {
                Player player = Program.playerManager.GetPlayer(connection);                
                Block block = Program.editor.GetBlock(packetBlock.UID);

                if (block != null && player != null)
                {
                    bool isSelected = Program.editor.IsSelected(packetBlock.UID);
                    bool selectedBySamePlayer = isSelected && Program.editor.IsSelectedBy(packetBlock.UID, player.SteamID);
                    bool notSelectedAccess = !isSelected && (block.SteamID == packet.SteamID || (byte)player.Permissions > 1);

                    if(selectedBySamePlayer || notSelectedAccess)
                    {
                        //Save the change
                        Program.editor.Update(packetBlock);

                        //Notify others
                        Program.playerManager.SendToAllExcept(connection, packet);
                        return;
                    }                 
                }
            }


            EditorBlockUpdateDeniedPacket editorBlockUpdateDeniedPacket = new EditorBlockUpdateDeniedPacket()
            {
                BlockString = Program.editor.GetBlockString(packetBlock.UID)
            };

            Program.playerManager.SendToPlayer(connection, editorBlockUpdateDeniedPacket);
            
        }

        public void HandleEditorBlockDestroy(EditorBlockDestroyPacket packet, NetConnection connection)
        {
            if (Access(packet.SteamID, connection, false))
            {
                Player player = Program.playerManager.GetPlayer(connection);
                Block block = Program.editor.GetBlock(packet.UID);

                if (block != null && player != null)
                {
                    bool isSelected = Program.editor.IsSelected(packet.UID);
                    bool selectedBySamePlayer = isSelected && Program.editor.IsSelectedBy(packet.UID, player.SteamID);
                    bool notSelectedAccess = !isSelected && (block.SteamID == packet.SteamID || (byte)player.Permissions > 1);

                    if (selectedBySamePlayer || notSelectedAccess)
                    {
                        //Save the change
                        Program.editor.Remove(packet.UID);

                        //Notify others
                        Program.playerManager.SendToAllExcept(connection, packet);
                        return;
                    }
                }
            }

            EditorBlockDestroyDeniedPacket editorBlockDestroyDeniedPacket = new EditorBlockDestroyDeniedPacket()
            {
                BlockString = Program.editor.GetBlockString(packet.UID)
            };

            Program.playerManager.SendToPlayer(connection, editorBlockDestroyDeniedPacket);            
        }

        public void HandleEditorFloor(EditorFloorPacket packet, NetConnection connection)
        {
            if (Access(packet.SteamID, connection, false))
            {
                //Save the change
                Program.editor.SetFloor(packet.Floor);

                //Notify others
                Program.playerManager.SendToAllExcept(connection, packet);
            }
            else
            {
                EditorFloorDeniedPacket editorFloorDeniedPacket = new EditorFloorDeniedPacket()
                {
                    Floor = Program.editor.Floor
                };

                Program.playerManager.SendToPlayer(connection, editorFloorDeniedPacket);
            }
        }

        public void HandleEditorSkybox(EditorSkyboxPacket packet, NetConnection connection)
        {
            if (Access(packet.SteamID, connection, false))
            {
                //Save the change
                Program.editor.SetSkybox(packet.Skybox);

                //Notify others
                Program.playerManager.SendToAllExcept(connection, packet);
            }
            else
            {
                EditorSkyboxDeniedPacket editorSkyboxDeniedPacket = new EditorSkyboxDeniedPacket()
                {
                    Skybox = Program.editor.Skybox
                };

                Program.playerManager.SendToPlayer(connection, editorSkyboxDeniedPacket);
            }
        }

        public void HandleEditorSelection(EditorSelectionPacket packet, NetConnection connection)
        {
            if (Access(packet.SteamID, connection, false))
            {
                Player player = Program.playerManager.GetPlayer(connection);
                Block block = Program.editor.GetBlock(packet.UID);

                if (block != null && player != null)
                {
                    if(!Program.editor.IsSelected(packet.UID))
                    {
                        if(block.SteamID == packet.SteamID || (byte)player.Permissions > 1)
                        {
                            Program.editor.Select(packet.UID, packet.SteamID);
                            return;
                        }
                    }                   
                }
            }          

            EditorSelectionDeniedPacket editorSelectionDenied = new EditorSelectionDeniedPacket()
            {
                UID = packet.UID
            };

            Program.playerManager.SendToPlayer(connection, editorSelectionDenied);
        }

        public void HandleEditorDeselection(EditorDeselectionPacket packet, NetConnection connection)
        {
            if (Access(packet.SteamID, connection, false))
            {
                Player player = Program.playerManager.GetPlayer(connection);
                Block block = Program.editor.GetBlock(packet.UID);

                if (block != null && player != null)
                {
                    if(Program.editor.IsSelectedBy(packet.UID, packet.SteamID))
                    {
                        Program.editor.Deselect(packet.UID);
                        return;
                    }                    
                }
            }
        }
        
        public void Shutdown(string message)
        {
            server?.Shutdown(message);
        }
      
    }
}
