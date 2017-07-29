using System;
namespace lotshared
{
    public class Player
    {
        public int X;
        public int Y;
        public long Id;

        public Player(long peerId)
        {
            this.Id = peerId;
            this.X = 100;
            this.Y = 100;
        }
    }
}
