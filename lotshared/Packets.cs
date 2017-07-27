using System;
namespace lotshared
{
    public class Packets
    {
        //from server
        public const byte PlayerPos = 0;
        public const byte Message = 1;
        public const byte Creature = 2;
        public const byte Shot = 3;
        public const byte SetPeerId = 4;

        //from client
        public const byte ClientMovement = 127;

        //testing flags
        public const bool SimulateLatency = false;
        public const int SimulationMinLatency = 50;
        public const int SimulationMaxLatency = 250;
        public const int SimulationPacketLossChance = 2;

        public Packets()
        {
        }
    }
}
