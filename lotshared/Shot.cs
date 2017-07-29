using System;
namespace lotshared
{
    public class Shot
    {
        public const int LifeSpan = 1000;
        public int Id;
        public int X;
		public int Y;
        public byte Type;
        public long SpawnTime;
        public int DirtyTime;
        public float Angle;
        public ShotFrame ShotFrame = new ShotFrame();
        public Shot()
        {
        }

        public bool IsDead(long serverTick, int extraTime)
        {
            return serverTick + extraTime > SpawnTime + LifeSpan;
        }

        internal void Update(long time)
        {
            if (time >= SpawnTime && time < SpawnTime + LifeSpan)
            {
                ShotFrame.Active = true;
                ShotFrame.X = X + (int)(Math.Cos(Angle) * 0.2d * (time - SpawnTime));
                ShotFrame.Y = Y + (int)(Math.Sin(Angle) * 0.2d * (time - SpawnTime));
            } else
            {
                ShotFrame.Active = false;
            }

        }
    }

    public class ShotFrame
    {
        public int X;
        public int Y;
        public bool Active;
    }
}
