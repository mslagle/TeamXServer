using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamXServer
{
    public class PlayerManager
    {
        public Dictionary<NetConnection, Player> connectedPlayers = new();
        
        public PlayerManager() { }

        public PermissionLevel GetPermissionLevel(ulong steamID)
        {
            return PermissionLevel.Moderator;
        }

        public Player AddPlayer(NetConnection connection, ulong steamID, PermissionLevel level)
        {
            Player player = new Player(connection, steamID, level);
            connectedPlayers.Add(connection, player);
            return player;
        }

        public void RemovePlayer(NetConnection connection)
        {
            connectedPlayers.Remove(connection);
        }

        public Player GetPlayer(NetConnection connection)
        {
            return connectedPlayers.TryGetValue(connection, out var player) ? player : null;
        }

        public void SendToAllPlayers<T>(T packet) where T : struct, IPacket
        {
            foreach (var connection in connectedPlayers.Keys)
            {
                SendToPlayer(connection, packet);
            }
        }

        public void SendToAllExcept<T>(NetConnection excludedConnection, T packet) where T : struct, IPacket
        {
            foreach (var connection in connectedPlayers.Keys)
            {
                if (connection != excludedConnection)
                {
                    SendToPlayer(connection, packet);
                }
            }
        }

        public void UpdatePlayer(NetConnection connection, PlayerStatePacket packet)
        {
            if (connectedPlayers.ContainsKey(connection))
            {
                Player player = GetPlayer(connection);
                player.PositionX = packet.PositionX;
                player.PositionY = packet.PositionY;
                player.PositionZ = packet.PositionZ;
                player.EulerX = packet.EulerX;
                player.EulerY = packet.EulerY; 
                player.EulerZ = packet.EulerZ;
                player.Mode = packet.Mode;
            }
        }

        public void SendToPlayer<T>(NetConnection connection, T packet) where T : struct, IPacket
        {
            // Create the outgoing message
            var outgoingMessage = connection.Peer.CreateMessage();

            // Serialize the packet with its type
            PacketUtility.Pack(packet, outgoingMessage);

            // Send the message
            connection.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 0);
        }
    }
}
