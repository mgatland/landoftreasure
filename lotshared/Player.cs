using System;
namespace lotshared
{
    public class Player
    {
        public int X;
        public int Y;
        public int Id;

        public Player(int id)
        {
            this.Id = id;
            this.X = 100;
            this.Y = 100;
        }
    }
}
