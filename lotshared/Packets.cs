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

        //from client
        public const byte ClientMovement = 127;

        //testing
        public const int SimulationMinLatency = 50-49;
        public const int SimulationMaxLatency = 100-99;
        public const int SimulationPacketLossChance = 2-1;

        public Packets()
        {
        }
    }
}
