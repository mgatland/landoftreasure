using System;
using System.Collections.Generic;

namespace lotshared
{
    public static class Shared
    {
        public static Player FindClosestPlayer(Creature c, List<Player> players)
        {
            int bestDistance = int.MaxValue;
            Player best = null;
            players.ForEach(p => {
                var distance = Distance(c, p);
                if (distance < bestDistance) {
                    bestDistance = distance;
                    best = p;
                }
            });
            return best;
        }

        public static int Distance(Creature c, Player p)
        {
            return (int)Math.Sqrt((c.X - p.X) * (c.X - p.X) + (c.Y - p.Y) * (c.Y - p.Y));
        }

        public static float AngleFromTo(Creature c, Player p)
        {
            float xDiff = p.X - c.X;
            float yDiff = p.Y - c.Y;
            return (float)Math.Atan2(yDiff, xDiff);
        }

        public static void UpdateShots(List<Shot> shots, long time)
        {
            foreach (var shot in shots)
            {
                shot.Update(time);
            }
        }
    }
}
