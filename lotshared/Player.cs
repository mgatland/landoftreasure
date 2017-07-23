using System;
namespace lotshared
{
    public class Player
    {
        public int x;
        public int y;
        public long peerId;

        public Player(long peerId)
        {
            this.peerId = peerId;
            this.x = 100;
            this.y = 100;
        }
    }
}
