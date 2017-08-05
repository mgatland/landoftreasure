﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lotshared
{
    public class QueuedMove
    {
        public long Tick;
        public int X;
        public int Y;
        public bool Charging;

        public QueuedMove(long tick, int x, int y, bool charging)
        {
            this.Tick = tick;
            this.X = x;
            this.Y = y;
            this.Charging = charging;
        }
    }
}
