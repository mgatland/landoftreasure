using System;
namespace lotshared
{
    public class Creature: GameObject
    {
        public int Id;
        public float angle;
        public int timer;
        public CreatureStatus Status = CreatureStatus.Alive;

        //Server only
        public long DeadTick = -1;

        public Creature()
        {
        }

        public bool IsDead(long serverTick, int extraTime)
        {
            return (Status == CreatureStatus.Dead && serverTick > DeadTick + extraTime);
        }
    }
}
