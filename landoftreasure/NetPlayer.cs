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
        public Dictionary<int, long> ShotKnowledge = new Dictionary<int, long>(); //shot ID, last update
        public List<QueuedMove> MoveQueue = new List<QueuedMove>();
        public LiteNetLib.NetPeer Peer;

        public NetPlayer(long peerId, LiteNetLib.NetPeer peer)
        {
            this.PeerId = peerId;
            this.Peer = peer;
        }
    }
}
