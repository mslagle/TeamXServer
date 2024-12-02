using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamXServer
{
    public enum PermissionLevel
    {
        Banned = 0,
        Player = 1,
        Moderator = 2,
        Admin = 3,
        Owner = 4
    }

    public interface IPacket
    {

        void Deserialize(NetIncomingMessage im);

        void Serialize(NetOutgoingMessage om);
    }

    public static class PacketUtility
    {
        private static readonly Dictionary<ushort, Type> PacketTypeRegistry = new();

        public static void AutoRegisterPacketsInSameNamespace()
        {
            string targetNamespace = typeof(PacketUtility).Namespace;

            var packetInterface = typeof(IPacket);

            var packetTypes = AppDomain.CurrentDomain
                                       .GetAssemblies()
                                       .SelectMany(a => a.GetTypes())
                                       .Where(t => t.Namespace != null &&
                                                   t.Namespace == targetNamespace &&
                                                   packetInterface.IsAssignableFrom(t) &&
                                                   t.IsValueType);

            foreach (var type in packetTypes)
            {
                RegisterPacketType(type);
            }
        }

        public static void RegisterPacketType(Type packetType)
        {
            ushort packetId = (ushort)(packetType.Name.GetStableHashCode() & ushort.MaxValue);
            PacketTypeRegistry[packetId] = packetType;
            Logger.Log($"Registering: {packetType.Name}, Packet ID: {packetId}");
        }

        public static Type GetPacketType(ushort packetId)
        {
            return PacketTypeRegistry.TryGetValue(packetId, out var type) ? type : null;
        }

        public static ushort GetPacketId<T>() where T : struct, IPacket
        {
            string typeName = typeof(T).Name;
            return (ushort)(typeName.GetStableHashCode() & ushort.MaxValue);
        }

        public static void Pack<T>(T packet, NetOutgoingMessage outgoingMessage) where T : struct, IPacket
        {
            ushort packetId = GetPacketId<T>();
            outgoingMessage.Write(packetId);
            packet.Serialize(outgoingMessage);
        }

        public static bool Unpack(NetIncomingMessage incomingMessage, out ushort msgType)
        {
            try
            {
                msgType = incomingMessage.ReadUInt16();
                return true;
            }
            catch (Exception ex)
            {
                msgType = 0;
                return false;
            }
        }
    }

    public struct HandshakeRequestPacket : IPacket
    {
        public string Message;

        public void Deserialize(NetIncomingMessage im)
        {
            Message = im.ReadString();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(Message);
        }
    }

    public struct HandshakeResponsePacket : IPacket
    {
        public ulong SteamID;

        public void Deserialize(NetIncomingMessage im)
        {
            SteamID = im.ReadUInt64();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(SteamID);
        }
    }

    public struct AccessGrantedPacket : IPacket
    {
        public string Message;
        public byte Level;

        public void Deserialize(NetIncomingMessage im)
        {
            Message = im.ReadString();
            Level = im.ReadByte();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(Message);
            om.Write(Level);
        }
    }

    public struct AccessDeniedPacket : IPacket
    {
        public string Reason;

        public void Deserialize(NetIncomingMessage im)
        {
            Reason = im.ReadString();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(Reason);
        }
    }

    public struct PlayerJoinPacket : IPacket
    {
        public ulong SteamID;
        public string Name;
        public int Zeepkist;
        public int FrontWheels;
        public int RearWheels;
        public int Paraglider;
        public int Horn;
        public int Hat;
        public int Glasses;
        public int Color_body;
        public int Color_leftArm;
        public int Color_rightArm;
        public int Color_leftLeg;
        public int Color_rightLeg;
        public int Color;

        public void Deserialize(NetIncomingMessage im)
        {
            SteamID = im.ReadUInt64();
            Name = im.ReadString();
            Zeepkist = im.ReadInt32();
            FrontWheels = im.ReadInt32();
            RearWheels = im.ReadInt32();
            Paraglider = im.ReadInt32();
            Horn = im.ReadInt32();
            Hat = im.ReadInt32();
            Glasses = im.ReadInt32();
            Color_body = im.ReadInt32();
            Color_leftArm = im.ReadInt32();
            Color_rightArm = im.ReadInt32();
            Color_leftLeg = im.ReadInt32();
            Color_rightLeg = im.ReadInt32();
            Color = im.ReadInt32();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(SteamID);
            om.Write(Name);
            om.Write(Zeepkist);
            om.Write(FrontWheels);
            om.Write(RearWheels);
            om.Write(Paraglider);
            om.Write(Horn);
            om.Write(Hat);
            om.Write(Glasses);
            om.Write(Color_body);
            om.Write(Color_leftArm);
            om.Write(Color_rightArm);
            om.Write(Color_leftLeg);
            om.Write(Color_rightLeg);
            om.Write(Color);
        }
    }

    public struct EditorStateRequestPacket : IPacket
    {
        public ulong SteamID;

        public void Deserialize(NetIncomingMessage im)
        {
            SteamID = im.ReadUInt64();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write((ulong)SteamID);
        }
    }

    public struct EditorStateResponsePacket : IPacket
    {
        public int Floor;
        public int Skybox;
        public int BlockCount;
        public List<string> BlockStrings;
        
        public void Deserialize(NetIncomingMessage om)
        {
            BlockStrings = new List<string>();

            Floor = om.ReadInt32();
            Skybox = om.ReadInt32();
            BlockCount = om.ReadInt32();
            for(int i = 0; i < BlockCount; i++)
            {
                BlockStrings.Add(om.ReadString());
            }
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(Floor);
            om.Write(Skybox);
            om.Write(BlockCount);
            foreach (string s in BlockStrings)
            {
                om.Write(s);
            }
        }
    }

    public struct EditorBlockCreatePacket : IPacket
    {
        public ulong SteamID;
        public string BlockString;

        public void Deserialize(NetIncomingMessage im)
        {
            SteamID = im.ReadUInt64();
            BlockString = im.ReadString();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(SteamID);
            om.Write(BlockString);
        }
    }

    public struct EditorBlockCreateDeniedPacket : IPacket
    {
        public string UID;

        public void Deserialize(NetIncomingMessage im)
        {
            UID = im.ReadString();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(UID);
        }
    }

    public struct EditorBlockDestroyPacket : IPacket
    {
        public ulong SteamID;
        public string UID;

        public void Deserialize(NetIncomingMessage im)
        {
            SteamID = im.ReadUInt64();
            UID = im.ReadString();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(SteamID);
            om.Write(UID);
        }
    }

    public struct EditorBlockDestroyDeniedPacket : IPacket
    {
        public string BlockString;

        public void Deserialize(NetIncomingMessage im)
        {
            BlockString = im.ReadString();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(BlockString);
        }
    }

    public struct EditorBlockUpdatePacket : IPacket
    {
        public ulong SteamID;
        public string BlockString;

        public void Deserialize(NetIncomingMessage im)
        {
            SteamID = im.ReadUInt64();
            BlockString = im.ReadString();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(SteamID);
            om.Write(BlockString);
        }
    }

    public struct EditorBlockUpdateDeniedPacket : IPacket
    {
        public string BlockString;

        public void Deserialize(NetIncomingMessage im)
        {
            BlockString = im.ReadString();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(BlockString);
        }
    }

    public struct EditorFloorPacket : IPacket
    {
        public ulong SteamID;
        public int Floor;

        public void Deserialize(NetIncomingMessage im)
        {
            SteamID = im.ReadUInt64();
            Floor = im.ReadInt32();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(SteamID);
            om.Write(Floor);
        }
    }

    public struct EditorFloorDeniedPacket : IPacket
    {
        public int Floor;

        public void Deserialize(NetIncomingMessage im)
        {
            Floor = im.ReadInt32();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(Floor);
        }
    }

    public struct EditorSkyboxPacket : IPacket
    {
        public ulong SteamID;
        public int Skybox;

        public void Deserialize(NetIncomingMessage im)
        {
            SteamID = im.ReadUInt64();
            Skybox = im.ReadInt32();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(SteamID);
            om.Write(Skybox);
        }
    }

    public struct EditorSkyboxDeniedPacket : IPacket
    {
        public int Skybox;

        public void Deserialize(NetIncomingMessage im)
        {
            Skybox = im.ReadInt32();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(Skybox);
        }
    }

    public struct EditorSelectionPacket : IPacket
    {
        public ulong SteamID;
        public string UID;

        public void Deserialize(NetIncomingMessage im)
        {
            SteamID = im.ReadUInt64();
            UID = im.ReadString();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(SteamID);
            om.Write(UID);
        }
    }

    public struct EditorSelectionDeniedPacket : IPacket
    {
        public string UID;

        public void Deserialize(NetIncomingMessage im)
        {
            UID = im.ReadString();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(UID);
        }
    }

    public struct EditorDeselectionPacket : IPacket
    {
        public ulong SteamID;
        public string UID;

        public void Deserialize(NetIncomingMessage im)
        {
            SteamID= im.ReadUInt64();
            UID = im.ReadString();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(SteamID);
            om.Write(UID);
        }
    }
    
    public struct PlayerLeftPacket : IPacket
    {
        public ulong SteamID;

        public void Deserialize(NetIncomingMessage im)
        {
            SteamID = im.ReadUInt64();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(SteamID);
        }
    }

    public struct PlayerStatePacket : IPacket
    {
        public ulong SteamID;
        public float PositionX;
        public float PositionY;
        public float PositionZ;
        public float EulerX;
        public float EulerY;
        public float EulerZ;
        public byte Mode;

        public void Deserialize(NetIncomingMessage im)
        {
            SteamID = im.ReadUInt64();
            PositionX = im.ReadFloat();
            PositionY = im.ReadFloat();
            PositionZ = im.ReadFloat();
            EulerX = im.ReadFloat();
            EulerY = im.ReadFloat();
            EulerZ = im.ReadFloat();
            Mode = im.ReadByte();
        }

        public void Serialize(NetOutgoingMessage om)
        {
            om.Write(SteamID);
            om.Write(PositionX);
            om.Write(PositionY);
            om.Write(PositionZ);
            om.Write(EulerX);
            om.Write(EulerY);
            om.Write(EulerZ);
            om.Write(Mode);
        }
    }
}
