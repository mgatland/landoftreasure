using lotshared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace landoftreasure
{
    public class NetPlayer
    {
        public long PeerId;
        public Player Player;
        public Player ClientSimPlayer;
        public Dictionary<int, long> ShotKnowledge = new Dictionary<int, long>(); //shot ID, last update
        public List<QueuedMove> MoveQueueUnverified = new List<QueuedMove>();
        public List<QueuedMove> MoveQueueVerified = new List<QueuedMove>();
        public LiteNetLib.NetPeer Peer;
        public long LastAckedMove;

        public int ReplayLatency;
        private int[] latencyRecords = new int[60];
        private int latencyI = 0;

        public NetPlayer(long peerId, LiteNetLib.NetPeer peer)
        {
            this.PeerId = peerId;
            this.Peer = peer;
        }

        internal void RecordLatency(int latency)
        {
            latencyRecords[latencyI++] = latency;
            if (latencyI == latencyRecords.Length)
            {
                latencyI = 0;
                Console.WriteLine("Player latency: " + latencyRecords.Max() + ", queue: " + MoveQueueUnverified.Count + "|" + MoveQueueVerified.Count);
            }
            //Smooth the replay delay
            ReplayLatency = (int)(Math.Round(ReplayLatency * 0.7d + latencyRecords.Max() * 0.3d));
        }
    }
}
