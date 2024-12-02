using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace TKServerConsole
{
    /*
    public class TKPlayer
    {
        public int ID;
        public string name;
        public byte state;

        public int zeepkist;
        public int frontWheels;
        public int rearWheels;
        public int paraglider;
        public int horn;
        public int hat;
        public int glasses;
        public int color_body;
        public int color_leftArm;
        public int color_rightArm;
        public int color_leftLeg;
        public int color_rightLeg;
        public int color;

        public NetConnection connection;
    }

    public static class TKPlayerManager
    {
        public static Dictionary<NetConnection, TKPlayer> players = new Dictionary<NetConnection, TKPlayer>();
        public static int connectionIDCounter = 100;

        public static void ProcessTransformDataMessage(NetConnection playerConnection, Vector3 position, Vector3 euler, byte state)
        {
            if(!players.ContainsKey(playerConnection))
            {
                Program.Log("Can't process transform data as player connection is not known!");
                return;
            }

            int playerID = players[playerConnection].ID;
                       
            if(players.Count <= 1)
            {
                //Theres only 1 player, no need to send it.
                return;
            }

            NetOutgoingMessage outgoingMessage = TKServer.server.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.PlayerTransformData);
            outgoingMessage.Write(playerID);
            outgoingMessage.Write(position.x);
            outgoingMessage.Write(position.y);
            outgoingMessage.Write(position.z);
            outgoingMessage.Write(euler.x);
            outgoingMessage.Write(euler.y);
            outgoingMessage.Write(euler.z);
            outgoingMessage.Write(state);
            SendMessageToAllPlayersExceptProvided(outgoingMessage, playerConnection);
        }

        public static void ProcessPlayerStateMessage(NetConnection playerConnection, byte state)
        {
            if (!players.ContainsKey(playerConnection))
            {
                Program.Log("Can't process transform data as player connection is not known!");
                return;
            }

            int playerID = players[playerConnection].ID;

            if (players.Count <= 1)
            {
                //Theres only 1 player, no need to send it.
                return;
            }

            NetOutgoingMessage outgoingMessage = TKServer.server.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.PlayerStateData);
            outgoingMessage.Write(playerID);
            outgoingMessage.Write(state);
            SendMessageToAllPlayersExceptProvided(outgoingMessage, playerConnection);
        }

        public static void PlayerLogIn(TKPlayer player)
        {
            if(players.ContainsKey(player.connection))
            {
                Program.Log("Player trying to log in is already logged in! Returning.");
                return;
            }

            //Create a new ID for the player and add it to the dictionary.
            connectionIDCounter++;
            player.ID = connectionIDCounter;
            players.Add(player.connection, player);

            Program.Log($"{player.name} joined the game!");

            //If there is only 1 player right now, we don't need to send any messages.
            if(players.Count == 1)
            {
                return;
            }

            //To the connecting player, send a message with all the information about the other players that are already online.
            NetOutgoingMessage serverPlayerDataMessage = CreateServerPlayerDataMessage(player.connection);     
            
            if(serverPlayerDataMessage != null)
            {
                //Send the message.
                SendMessageToSinglePlayer(serverPlayerDataMessage, player.connection);
            }

            //To all the other players, send a message with the information about the current connecting player.
            NetOutgoingMessage joinedPlayerDataMessage = CreateJoinedPlayerDataMessage(player.connection);
            SendMessageToAllPlayersExceptProvided(joinedPlayerDataMessage, player.connection);
        }   
        
        public static NetOutgoingMessage CreateServerPlayerDataMessage(NetConnection exclude)
        {
            List<NetConnection> connections = players.Keys.Where(connection => connection != exclude).ToList();

            if(connections.Count == 0)
            {
                //Shouldn't happen but just in case.
                return null;
            }

            NetOutgoingMessage outgoingMessage = TKServer.server.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.ServerPlayerData);
            outgoingMessage.Write(connections.Count);

            //Foreach connections write the data in the message.
            foreach(NetConnection connection in connections)
            {
                TKPlayer p = players[connection];
                outgoingMessage.Write(p.ID);
                outgoingMessage.Write(p.state);
                outgoingMessage.Write(p.name);
                outgoingMessage.Write(p.zeepkist);
                outgoingMessage.Write(p.frontWheels);
                outgoingMessage.Write(p.rearWheels);
                outgoingMessage.Write(p.paraglider);
                outgoingMessage.Write(p.horn);
                outgoingMessage.Write(p.hat);
                outgoingMessage.Write(p.glasses);
                outgoingMessage.Write(p.color_body);
                outgoingMessage.Write(p.color_leftArm);
                outgoingMessage.Write(p.color_rightArm);
                outgoingMessage.Write(p.color_leftLeg);
                outgoingMessage.Write(p.color_rightLeg);
                outgoingMessage.Write(p.color);
            }

            return outgoingMessage;
        }

        public static NetOutgoingMessage CreateJoinedPlayerDataMessage(NetConnection joinedConnection)
        {
            TKPlayer p = players[joinedConnection];
            NetOutgoingMessage outgoingMessage = TKServer.server.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.JoinedPlayerData);
            outgoingMessage.Write(p.ID);
            outgoingMessage.Write(p.state);
            outgoingMessage.Write(p.name);
            outgoingMessage.Write(p.zeepkist);
            outgoingMessage.Write(p.frontWheels);
            outgoingMessage.Write(p.rearWheels);
            outgoingMessage.Write(p.paraglider);
            outgoingMessage.Write(p.horn);
            outgoingMessage.Write(p.hat);
            outgoingMessage.Write(p.glasses);
            outgoingMessage.Write(p.color_body);
            outgoingMessage.Write(p.color_leftArm);
            outgoingMessage.Write(p.color_rightArm);
            outgoingMessage.Write(p.color_leftLeg);
            outgoingMessage.Write(p.color_rightLeg);
            outgoingMessage.Write(p.color);

            return outgoingMessage;
        }
        
        public static void PlayerLogOut(NetConnection connection)
        {
            if (!players.ContainsKey(connection))
            {
                Program.Log("Player trying to log out is not registered! Returning.");
                return;
            }

            TKPlayer p = players[connection];
            players.Remove(connection);

            Program.Log($"{p.name} left the game!");

            //To all the other players, send a message with the ID of the player that left.
            NetOutgoingMessage playerLeftMessage = CreatePlayerLeftMessage(p.ID);
            SendMessageToAllPlayers(playerLeftMessage);
        }

        public static NetOutgoingMessage CreatePlayerLeftMessage(int leavingID)
        {
            NetOutgoingMessage outgoingMessage = TKServer.server.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.PlayerLeft);
            outgoingMessage.Write(leavingID);
            return outgoingMessage;
        }

        public static void SendMessageToSinglePlayer(NetOutgoingMessage outgoingMessage, NetConnection connection)
        {
            TKServer.server.SendMessage(outgoingMessage, connection, NetDeliveryMethod.ReliableOrdered);
        }

        public static void SendMessageToAllPlayers(NetOutgoingMessage outgoingMessage)
        {
            List<NetConnection> connections = players.Keys.ToList();

            if (connections.Count <= 0)
            {
                return;
            }

            TKServer.server.SendMessage(outgoingMessage, connections, NetDeliveryMethod.ReliableOrdered, 0);
        }    

        public static void SendMessageToAllPlayersExceptProvided(NetOutgoingMessage outgoingMessage, NetConnection excludedConnection)
        {
            List<NetConnection> connections = players.Keys.Where(connection => connection != excludedConnection).ToList();

            if(connections.Count <= 0)
            {
                return;
            }

            TKServer.server.SendMessage(outgoingMessage, connections, NetDeliveryMethod.ReliableOrdered, 0);
        }
    }*/
}
