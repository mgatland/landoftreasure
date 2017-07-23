using System;
namespace lotshared
{
    public class Player
    {
        public int X;
        public int Y;
        public long PeerId;

        public Player(long peerId)
        {
            this.PeerId = peerId;
            this.X = 100;
            this.Y = 100;
        }
    }
}
