using System;
namespace lotshared
{
    public class Player: GameObject
    {
        public int Id;
        public int Health;
        public int MaxHealth;
        public int Charge;
        public int MaxCharge;


        public Player(int id)
        {
            this.Id = id;
            this.X = 100;
            this.Y = 100;
            this.MaxHealth = 100;
            this.Health = 100;
            this.MaxCharge = 100;
            this.Charge = 0;
        }
    }
}
