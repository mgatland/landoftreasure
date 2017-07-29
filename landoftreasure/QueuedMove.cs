using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace landoftreasure
{
    public class QueuedMove
    {
        public long Tick;
        public int X;
        public int Y;

        public QueuedMove(long tick, int x, int y)
        {
            this.Tick = tick;
            this.X = x;
            this.Y = y;
        }
    }
}
