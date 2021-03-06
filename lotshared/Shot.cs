﻿using System;
namespace lotshared
{
    public class Shot: GameObject
    {
        public const int LifeSpan = 1000;
        public int Id;
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
            return serverTick > SpawnTime + LifeSpan + extraTime;
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

    public class ShotFrame: GameObject
    {
        public bool Active;
    }
}
