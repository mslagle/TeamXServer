using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamXServer
{
    public class Player
    {
        public NetConnection Connection { get; }

        public ulong SteamID { get; set; }
        public string Name { get; set; }
        public int Zeepkist { get; set; }
        public int FrontWheels { get; set; }
        public int RearWheels { get; set; }
        public int Paraglider { get; set; }
        public int Horn { get; set; }
        public int Hat { get; set; }
        public int Glasses { get; set; }
        public int Color_body { get; set; }
        public int Color_leftArm { get; set; }
        public int Color_rightArm { get; set; }
        public int Color_leftLeg { get; set; }
        public int Color_rightLeg { get; set; }
        public int Color { get; set; }

        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float EulerX { get; set; }
        public float EulerY { get; set; }
        public float EulerZ { get; set; }
        public byte Mode { get; set; }

        public Player(NetConnection connection, ulong steamID)
        {
            SteamID = steamID;
            Connection = connection;
        }

        public void SetProperties(PlayerJoinPacket packet)
        {
            Name = packet.Name;
            Zeepkist = packet.Zeepkist;
            FrontWheels = packet.FrontWheels;
            RearWheels = packet.RearWheels;
            Paraglider = packet.Paraglider;
            Horn = packet.Horn;
            Hat = packet.Hat;
            Glasses = packet.Glasses;
            Color_body = packet.Color_body;
            Color_leftArm = packet.Color_leftArm;
            Color_rightArm = packet.Color_rightArm;
            Color_leftLeg = packet.Color_leftLeg;
            Color_rightLeg = packet.Color_rightLeg;
            Color = packet.Color;
        }
    }

}
