﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lotshared
{
    public class QueuedMove
    {
        public int Tick;
        public int X;
        public int Y;

        public QueuedMove(long tick, int x, int y)
        {
            this.Tick = (int)tick; //FIXME cast!
            this.X = x;
            this.Y = y;
        }
    }
}
