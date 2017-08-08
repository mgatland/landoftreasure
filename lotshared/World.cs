using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lotshared
{
    public interface World
    {
        List<Player> Players { get; }
        void SpawnShot(Creature creature, float angle);
    }
}
