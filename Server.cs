using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TeamXNetwork;
using Newtonsoft.Json;
using ZeepkistNetworking;

namespace TeamXServer
{
    public class ServerJSON
    {
        public string ServerIP { get; set; }
        public int ServerPort { get; set; }
    }

    public class Server
    {
        private NetPeerConfiguration config;
        private NetServer server;
        public ServerJSON serverConfiguration;
        public string _filePath;

        public Server()
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "userdata", "server.json");

            serverConfiguration = ReadConfig();
            if(serverConfiguration == null)
            {
                serverConfiguration = new ServerJSON()
                {
                    ServerIP = "127.0.0.1",
                    ServerPort = 8080
                };

                string jsonContent = JsonConvert.SerializeObject(serverConfiguration, Formatting.Indented);
                File.WriteAllText(_filePath, jsonContent);
            }

            config = new NetPeerConfiguration("TeamX");
            config.Port = serverConfiguration.ServerPort;
            config.LocalAddress = IPAddress.Any;

            try
            {
                server = new NetServer(config);
                server.Start();
            }
            catch (Exception e)
            {
                Logger.Log("A problem occured when setting up the server. Please check configuration.", LogType.Error);
                Logger.Log(e.ToString(), LogType.Error);
                throw;
            }

            Logger.Log("Starting server on " + serverConfiguration.ServerIP + ":" + serverConfiguration.ServerPort, LogType.Message);
        }

        public ServerJSON ReadConfig()
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            string jsonContent = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<ServerJSON>(jsonContent);
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
                                var packet = (TeamXNetwork.IPacket)Activator.CreateInstance(packetType);
                                packet.Deserialize(im);
                                Logger.Log($"Received packet of type: {packetType.Name}", LogType.Debug);
                                HandlePacket(packet, senderConnection);
                            }
                            else
                            {
                                Logger.Log($"Unknown packet ID: {packetId}", LogType.Warning);
                            }
                        }
                        else
                        {
                            Logger.Log("Failed to unpack the message.", LogType.Warning);
                        }
                        break;
                }
            }
        }

        public void HandlePacket(TeamXNetwork.IPacket packet, NetConnection connection)
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
                case ServerRulesRequestPacket serverRulesRequestPacket:
                    HandleServerRulesRequest(serverRulesRequestPacket, connection);
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
                case PermissionTableRequest permissionTableRequest:
                    HandlePermissionTableRequest(permissionTableRequest, connection);
                    break;
                case PermissionTableSubmit permissionTableSubmit:
                    HandlePermissionTableSubmit(permissionTableSubmit, connection);
                    break;
                case LevelDirectoryRequestPacket levelDirectoryRequestPacket:
                    HandleLevelDirectoryRequest(levelDirectoryRequestPacket, connection);
                    break;
                case LoadLevelRequestPacket loadLevelRequestPacket:
                    HandleLoadLevelRequest(loadLevelRequestPacket, connection);
                    break;
                case SaveConfigurationRequestPacket saveConfigurationRequestPacket:
                    HandleSaveConfigurationRequest(saveConfigurationRequestPacket, connection);
                    break;
                case SaveConfigurationSubmitPacket saveConfigurationSubmitPacket:
                    HandleSaveConfigurationSubmit(saveConfigurationSubmitPacket, connection);
                    break;
                case SaveCurrentStatePacket saveCurrentStatePacket:
                    HandleSaveCurrentState(saveCurrentStatePacket, connection);
                    break;
            }
        }

        public void HandleLevelDirectoryRequest(LevelDirectoryRequestPacket levelDirectoryRequest, NetConnection connection)
        {
            PermissionEntry perms = Program.perms.GetPermissions(levelDirectoryRequest.SteamID);
            if (perms.IsAdministrator)
            {
                LevelDirectoryResponsePacket levelDirectoryResponse = new LevelDirectoryResponsePacket();
                levelDirectoryResponse.LocalPaths = new List<string>();

                // Find all .teamkist files in ServerBasePath and subfolders
                IEnumerable<string> teamkistFiles = Directory.EnumerateFiles(
                     Path.Combine(Program.saveManager.ServerBasePath, "userdata"),
                     "*.teamkist",
                     SearchOption.AllDirectories
                 );

                foreach (var file in teamkistFiles)
                {
                    // Compute the local path relative to 'userdata'
                    string localPath = Path.GetRelativePath(
                        Path.Combine(Program.saveManager.ServerBasePath, "userdata"),
                        file
                    );

                    levelDirectoryResponse.LocalPaths.Add(localPath);
                }

                // Prepare and send the response packet
                var outgoingMessage = connection.Peer.CreateMessage();
                PacketUtility.Pack(levelDirectoryResponse, outgoingMessage);
                connection.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        public void HandleLoadLevelRequest(LoadLevelRequestPacket levelLoadRequest, NetConnection connection)
        {
            PermissionEntry perms = Program.perms.GetPermissions(levelLoadRequest.SteamID);
            if (perms.IsAdministrator)
            {
                string fullPath = Path.Combine(Program.saveManager.ServerBasePath, "userdata", levelLoadRequest.LocalPath);

                // Look for the .teamkist file in the subdirectory or directory.
                FileInfo fileInfo = null;

                if (File.Exists(fullPath))
                {
                    // If the file exists, get its FileInfo
                    fileInfo = new FileInfo(fullPath);

                    //Clear the editor.
                    Program.editor.Clear();

                    //Load the file into the editor.
                    Program.saveManager.LoadSave(fileInfo);

                    //Set the save configuration level name to the loaded project.
                    SaveJSON saveJSON = Program.saveManager.ReadConfig();
                    string[] parts = levelLoadRequest.LocalPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string projectName = parts[0];
                    if(!string.IsNullOrEmpty(projectName))
                    {
                        saveJSON.LevelName = projectName;
                        Program.saveManager.ApplySaveConfiguration(saveJSON);
                    }                        

                    //Send a ServerStateResponse to all connected players.
                    foreach (KeyValuePair<NetConnection, Player> kvp in Program.playerManager.connectedPlayers)
                    {
                        EditorStateResponsePacket editorState = new EditorStateResponsePacket()
                        {
                            BlockCount = Program.editor.Blocks.Count,
                            BlockStrings = Program.editor.GetBlockStrings(),
                            Floor = Program.editor.Floor,
                            Skybox = Program.editor.Skybox
                        };

                        var outgoingMessage = kvp.Key.Peer.CreateMessage();
                        PacketUtility.Pack(editorState, outgoingMessage);
                        kvp.Key.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 0);

                        Logger.Log($"Sending EditorStateResponse packet back to player.", LogType.Debug);
                    }
                }
            }
        }

        public void HandleSaveConfigurationRequest(SaveConfigurationRequestPacket saveConfigurationRequest, NetConnection connection)
        {
            PermissionEntry perms = Program.perms.GetPermissions(saveConfigurationRequest.SteamID);
            if (perms.IsAdministrator)
            {
                SaveJSON saveJSON = Program.saveManager.saveConfiguration;
                SaveConfigurationResponsePacket saveConfigurationResponse = new SaveConfigurationResponsePacket()
                {
                    AutoSaveInterval = saveJSON.AutoSaveInterval,
                    BackupCount = saveJSON.BackupCount,
                    KeepBackupWithNoEditors = saveJSON.KeepBackupWithNoEditors,
                    LevelName = saveJSON.LevelName,
                    LoadBackupOnStart = saveJSON.LoadBackupOnStart
                };

                var outgoingMessage = connection.Peer.CreateMessage();
                PacketUtility.Pack(saveConfigurationResponse, outgoingMessage);
                connection.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        public void HandleSaveConfigurationSubmit(SaveConfigurationSubmitPacket saveConfigurationSubmit, NetConnection connection)
        {
            PermissionEntry perms = Program.perms.GetPermissions(saveConfigurationSubmit.SteamID);
            if (perms.IsAdministrator)
            {
                SaveJSON saveJSON = new SaveJSON()
                {
                    AutoSaveInterval = saveConfigurationSubmit.AutoSaveInterval,
                    BackupCount = saveConfigurationSubmit.BackupCount,
                    LoadBackupOnStart = saveConfigurationSubmit.LoadBackupOnStart,
                    LevelName = saveConfigurationSubmit.LevelName,
                    KeepBackupWithNoEditors = saveConfigurationSubmit.KeepBackupWithNoEditors
                };
                
                Program.saveManager.ApplySaveConfiguration(saveJSON);
            }
        }

        public void HandleSaveCurrentState(SaveCurrentStatePacket saveCurrentState, NetConnection connection)
        {
            PermissionEntry perms = Program.perms.GetPermissions(saveCurrentState.SteamID);
            if (perms.IsAdministrator)
            {
                Program.saveManager.Save();
            }
        }

        public void HandlePermissionTableRequest(PermissionTableRequest tableRequest, NetConnection connection)
        {
            PermissionEntry perms = Program.perms.GetPermissions(tableRequest.SteamID);
            if (perms.IsAdministrator)
            {
                PermissionTableResponse resp = new PermissionTableResponse();
                resp.permissionTable = new List<(ulong, string, string)>();
                
                //Create a table and send it back
                //Go over all players
                foreach(Player p in Program.playerManager.connectedPlayers.Values)
                {
                    resp.permissionTable.Add(Program.perms.GetPermissionEntry(p.SteamID));
                }

                var outgoingMessage = connection.Peer.CreateMessage();
                PacketUtility.Pack(resp, outgoingMessage);
                connection.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        public void HandlePermissionTableSubmit(PermissionTableSubmit tableSubmit, NetConnection connection)
        {
            foreach((ulong,string,string) entry in tableSubmit.permissionTable)
            {
                Program.perms.UpdatePlayerPermission(entry.Item1.ToString(), entry.Item3);
            }

            //For each connected player, send them the permissions according to their permission level.
            foreach(KeyValuePair<NetConnection, Player> kvp in Program.playerManager.connectedPlayers)
            {
                ulong steamID = kvp.Value.SteamID;

                PermissionEntry perms = Program.perms.GetPermissions(steamID);

                ServerRulesResponsePacket serverRules = new ServerRulesResponsePacket()
                {
                    IsAdministrator = perms.IsAdministrator,
                    CanJoin = perms.CanJoin,
                    CanCreate = perms.CanCreate,
                    CanEdit = perms.CanEdit,
                    CanEditAll = perms.CanEditAll,
                    CanEditFloor = perms.CanEditFloor,
                    CanEditSkybox = perms.CanEditSkybox,
                    CanDestroy = perms.CanDestroy,
                    BlockLimit = perms.BlockLimit,
                    BannedBlocks = perms.BannedBlocks
                };

                var outgoingMessage = kvp.Key.Peer.CreateMessage();
                PacketUtility.Pack(serverRules, outgoingMessage);
                kvp.Key.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 0);

                Logger.Log($"Sending ServerRulesResponse packet back to player.", LogType.Debug);
            }
        }

        public bool Access(ulong steamID, NetConnection connection, bool sendAccessDenied = true)
        {
            PermissionEntry perms = Program.perms.GetPermissions(steamID);
            if(!perms.CanJoin)
            {
                if (sendAccessDenied)
                {
                    Logger.Log($"Sending AccessDenied packet to {steamID}.", LogType.Debug);

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
            Logger.Log("Player connected...", LogType.Message);

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
                Logger.Log($"Player {player.SteamID} disconnected.", LogType.Message);

                PlayerLeftPacket playerLeftPacket = new PlayerLeftPacket()
                {
                    SteamID = player.SteamID
                };

                HandlePlayerLeft(playerLeftPacket, connection);
            }
        }

        public void HandleHandshakeResponse(HandshakeResponsePacket packet, NetConnection connection)
        {
            Logger.Log($"Received HandleHandshakeResponse packet from {packet.SteamID}.", LogType.Debug);

            if(Access(packet.SteamID, connection))
            {
                AccessGrantedPacket accessGrantedPacket = new AccessGrantedPacket()
                {
                    Message = "Access Granted."
                };

                Logger.Log($"Sending AccessGrantedPacket to {packet.SteamID}.", LogType.Debug);

                var outgoingMessage = connection.Peer.CreateMessage();
                PacketUtility.Pack(accessGrantedPacket, outgoingMessage);
                connection.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        public void HandlePlayerJoin(PlayerJoinPacket packet, NetConnection connection)
        {
            Logger.Log($"Received HandlePlayerJoin packet from {packet.SteamID}", LogType.Debug);
            Logger.Log($"Player {packet.SteamID} joined", LogType.Message);
            
            if (Access(packet.SteamID, connection))
            {
                Player player = Program.playerManager.AddPlayer(connection, packet.SteamID);
                player.SetProperties(packet);

                //Rename the player in the permission system.
                Program.perms.UpdatePlayerName(packet.SteamID.ToString(), player.Name);
                
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

                Logger.Log($"Send PlayerJoinPacket to all other connected players.", LogType.Debug);
            }
        }

        public void HandlePlayerLeft(PlayerLeftPacket packet, NetConnection connection)
        {
            Logger.Log($"Received PlayerLeft packet from {packet.SteamID}.", LogType.Debug);
            Program.playerManager.RemovePlayer(connection);
            Program.editor.RemoveAllSelectionsFrom(packet.SteamID);

            Logger.Log($"Sending PlayerLeft packet to all other connected players.", LogType.Debug);
            Program.playerManager.SendToAllExcept(connection, packet);
        }
        
        public void HandlePlayerState(PlayerStatePacket packet, NetConnection connection)
        {
            Logger.Log($"Received PlayerState packet from {packet.SteamID}.", LogType.Debug);

            if (Access(packet.SteamID, connection))
            {              
                Program.playerManager.UpdatePlayer(connection, packet);

                Logger.Log($"Sending PlayerState packet to all other connected players.", LogType.Debug);
                Program.playerManager.SendToAllExcept(connection, packet);
            }
        }
        
        public void HandleEditorStateRequest(EditorStateRequestPacket packet, NetConnection connection)
        {
            Logger.Log($"Received EditorStateRequest packet from {packet.SteamID}.", LogType.Debug);

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

                Logger.Log($"Sending EditorStateResponse packet back to player.", LogType.Debug);
            }
        }

        public void HandleServerRulesRequest(ServerRulesRequestPacket packet, NetConnection connection)
        {
            Logger.Log($"Received ServerRulesRequest packet from {packet.SteamID}.", LogType.Debug);

            if(Access(packet.SteamID, connection))
            {
                PermissionEntry perms = Program.perms.GetPermissions(packet.SteamID);

                ServerRulesResponsePacket serverRules = new ServerRulesResponsePacket()
                {
                    IsAdministrator = perms.IsAdministrator,
                    CanJoin = perms.CanJoin,
                    CanCreate = perms.CanCreate,
                    CanEdit = perms.CanEdit,
                    CanEditAll = perms.CanEditAll,
                    CanEditFloor = perms.CanEditFloor,
                    CanEditSkybox = perms.CanEditSkybox,
                    CanDestroy = perms.CanDestroy,
                    BlockLimit = perms.BlockLimit,
                    BannedBlocks = perms.BannedBlocks
                };

                var outgoingMessage = connection.Peer.CreateMessage();
                PacketUtility.Pack(serverRules, outgoingMessage);
                connection.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 0);

                Logger.Log($"Sending ServerRulesResponse packet back to player.", LogType.Debug);
            }
        }

        public void HandleEditorBlockCreate(EditorBlockCreatePacket packet, NetConnection connection)
        {
            Logger.Log($"Received EditorBlockCreate packet from {packet.SteamID}.", LogType.Debug);

            Block packetBlock = Program.editor.JSONToBlock(packet.BlockString);

            if (Access(packet.SteamID, connection, false))
            {
                //Save the change
                Program.editor.Add(packetBlock);

                Logger.Log($"Sending EditorBlockCreate packet to all other connected players.", LogType.Debug);

                //Notify others
                Program.playerManager.SendToAllExcept(connection, packet);
            }
            else
            {
                Logger.Log($"Received EditorBlockCreate packet from {packet.SteamID}, but permission level is not high enough.", LogType.Debug);

                EditorBlockCreateDeniedPacket editorBlockCreateDeniedPacket = new EditorBlockCreateDeniedPacket()
                {
                     UID = packetBlock.UID
                };

                Logger.Log($"Sending EditorBlockCreateDenied packet back to player.", LogType.Debug);

                Program.playerManager.SendToPlayer(connection, editorBlockCreateDeniedPacket);
            }
        }

        public void HandleEditorBlockUpdate(EditorBlockUpdatePacket packet, NetConnection connection)
        {
            Logger.Log($"Received EditorBlockUpdate packet from {packet.SteamID}.", LogType.Debug);

            Block packetBlock = Program.editor.JSONToBlock(packet.BlockString);

            if (Access(packet.SteamID, connection, false))
            {
                Player player = Program.playerManager.GetPlayer(connection);                
                Block block = Program.editor.GetBlock(packetBlock.UID);
                PermissionEntry perms = Program.perms.GetPermissions(packet.SteamID);

                if (block != null && player != null)
                {
                    bool isSelected = Program.editor.IsSelected(packetBlock.UID);
                    bool selectedBySamePlayer = isSelected && Program.editor.IsSelectedBy(packetBlock.UID, player.SteamID);
                    bool notSelectedAccess = !isSelected && (block.SteamID == packet.SteamID || perms.CanEditAll);

                    Logger.Log($"HandleEditorBlockUpdate: IsSelected: {isSelected}, SelectedBySamePlayer: {selectedBySamePlayer}, NotSelectedAccess: {notSelectedAccess}.", LogType.Debug);

                    if (selectedBySamePlayer || notSelectedAccess)
                    {
                        Logger.Log($"HandleEditorBlockUpdate: Updating block and notifying others.", LogType.Debug);

                        //Save the change
                        Program.editor.Update(packetBlock);

                        //Notify others
                        Program.playerManager.SendToAllExcept(connection, packet);
                        return;
                    }                 
                }
                else
                {
                    Logger.Log($"HandleEditorBlockUpdate: Either block or player returned null. Player {player}, Block: {block}", LogType.Debug);
                }
            }

            Logger.Log($"Sending EditorBlockUpdateDenied packet back to player.", LogType.Debug);

            EditorBlockUpdateDeniedPacket editorBlockUpdateDeniedPacket = new EditorBlockUpdateDeniedPacket()
            {
                BlockString = Program.editor.GetBlockString(packetBlock.UID)
            };

            Program.playerManager.SendToPlayer(connection, editorBlockUpdateDeniedPacket);            
        }

        public void HandleEditorBlockDestroy(EditorBlockDestroyPacket packet, NetConnection connection)
        {
            Logger.Log($"Received EditorBlockDestroy packet from {packet.SteamID}.", LogType.Debug);

            if (Access(packet.SteamID, connection, false))
            {
                Player player = Program.playerManager.GetPlayer(connection);
                Block block = Program.editor.GetBlock(packet.UID);
                PermissionEntry perms = Program.perms.GetPermissions(packet.SteamID);

                if (block != null && player != null)
                {
                    bool isSelected = Program.editor.IsSelected(packet.UID);
                    bool selectedBySamePlayer = isSelected && Program.editor.IsSelectedBy(packet.UID, player.SteamID);
                    bool notSelectedAccess = !isSelected && (block.SteamID == packet.SteamID || perms.CanEditAll);

                    Logger.Log($"HandleEditorBlockDestroy: IsSelected: {isSelected}, SelectedBySamePlayer: {selectedBySamePlayer}, NotSelectedAccess: {notSelectedAccess}.", LogType.Debug);

                    if (selectedBySamePlayer || notSelectedAccess)
                    {
                        Logger.Log($"HandleEditorBlockDestroy: Destroying block and notifying others.", LogType.Debug);

                        //Save the change
                        Program.editor.Remove(packet.UID);

                        //Notify others
                        Program.playerManager.SendToAllExcept(connection, packet);

                        //Make sure selections are removed from destroyed blocks.
                        Program.editor.Deselect(packet.UID);
                        return;
                    }
                }
                else
                {
                    Logger.Log($"HandleEditorBlockDestroy: Either block or player returned null. Player {player}, Block: {block}", LogType.Debug);
                }
            }

            Logger.Log($"Sending EditorBlockDestroyDenied packet back to player.", LogType.Debug);

            EditorBlockDestroyDeniedPacket editorBlockDestroyDeniedPacket = new EditorBlockDestroyDeniedPacket()
            {
                BlockString = Program.editor.GetBlockString(packet.UID)
            };

            Program.playerManager.SendToPlayer(connection, editorBlockDestroyDeniedPacket);            
        }

        public void HandleEditorFloor(EditorFloorPacket packet, NetConnection connection)
        {
            Logger.Log($"Received EditorFloor packet from {packet.SteamID}.", LogType.Debug);

            if (Access(packet.SteamID, connection, false))
            {
                Logger.Log($"HandleEditorFloor: Setting floor and notifying others.", LogType.Debug);

                //Save the change
                Program.editor.SetFloor(packet.Floor);

                //Notify others
                Program.playerManager.SendToAllExcept(connection, packet);
            }
            else
            {
                Logger.Log($"Sending EditorFloorDenied packet back to player.", LogType.Debug);

                EditorFloorDeniedPacket editorFloorDeniedPacket = new EditorFloorDeniedPacket()
                {
                    Floor = Program.editor.Floor
                };

                Program.playerManager.SendToPlayer(connection, editorFloorDeniedPacket);
            }
        }

        public void HandleEditorSkybox(EditorSkyboxPacket packet, NetConnection connection)
        {
            Logger.Log($"Received EditorSkybox packet from {packet.SteamID}.", LogType.Debug);

            if (Access(packet.SteamID, connection, false))
            {
                Logger.Log($"HandleEditorSkybox: Setting skybox and notifying others.", LogType.Debug);

                //Save the change
                Program.editor.SetSkybox(packet.Skybox);

                //Notify others
                Program.playerManager.SendToAllExcept(connection, packet);
            }
            else
            {
                Logger.Log($"Sending EditorSkyboxDenied packet back to player.", LogType.Debug);

                EditorSkyboxDeniedPacket editorSkyboxDeniedPacket = new EditorSkyboxDeniedPacket()
                {
                    Skybox = Program.editor.Skybox
                };

                Program.playerManager.SendToPlayer(connection, editorSkyboxDeniedPacket);
            }
        }

        public void HandleEditorSelection(EditorSelectionPacket packet, NetConnection connection)
        {
            Logger.Log($"Received EditorSelection packet from {packet.SteamID}.", LogType.Debug);

            if (Access(packet.SteamID, connection, false))
            {
                Player player = Program.playerManager.GetPlayer(connection);
                Block block = Program.editor.GetBlock(packet.UID);
                PermissionEntry perms = Program.perms.GetPermissions(packet.SteamID);

                if (block != null && player != null)
                {
                    if(!Program.editor.IsSelected(packet.UID))
                    {
                        if(block.SteamID == packet.SteamID || perms.CanEditAll)
                        {
                            Program.editor.Select(packet.UID, packet.SteamID);
                            return;
                        }
                    }
                    else if(Program.editor.IsSelectedBy(packet.UID, packet.SteamID))
                    {
                        Logger.Log("HandleEditorSelection: Double Selection... CTRL-YZ?", LogType.Debug);
                        return;
                    }
                    else
                    {
                        Logger.Log($"HandleEditorSelection: Block is already selected by {Program.editor.SelectedByWho(packet.UID)}.", LogType.Debug);
                    }
                }
                else
                {
                    Logger.Log($"HandleEditorSelection: Either block or player returned null. Player {player}, Block: {block}", LogType.Debug);
                }
            }

            Logger.Log($"Sending EditorSelectionDenied packet back to player.", LogType.Debug);

            EditorSelectionDeniedPacket editorSelectionDenied = new EditorSelectionDeniedPacket()
            {
                UID = packet.UID
            };

            Program.playerManager.SendToPlayer(connection, editorSelectionDenied);
        }

        public void HandleEditorDeselection(EditorDeselectionPacket packet, NetConnection connection)
        {
            Logger.Log($"Received EditorDeselection packet from {packet.SteamID}.", LogType.Debug);

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
                    else
                    {
                        Logger.Log($"HandleEditorDeselection: Trying to deselect a block that is not owned by this player.", LogType.Debug);
                    }
                }
                else
                {
                    Logger.Log($"HandleEditorDeselection: Either block or player returned null. Player {player}, Block: {block}", LogType.Debug);
                }
            }
        }
        
        public void Shutdown(string message)
        {
            server?.Shutdown(message);
        }      
    }
}
