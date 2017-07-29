using System;
namespace lotshared
{
    public class Packets
    {
        //from server
        //nope public const byte PlayerPos = 0;
        public const byte Message = 1;
        public const byte Snapshot = 2;
        public const byte Shot = 3;
        public const byte WelcomeClient = 4;

        //from client
        public const byte ClientMovement = 127;

        //testing flags
        public const bool SimulateLatency = true;
        public const int SimulationMinLatency = 200;
        public const int SimulationMaxLatency = 250;
        public const bool SimulatePacketLoss = true;
        public const int SimulationPacketLossChance = 1;

        public Packets()
        {
        }
    }
}
