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

        public static int Distance(GameObject a, GameObject b)
        {
            return (int)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }

        public static float AngleFromTo(GameObject a, GameObject b)
        {
            float xDiff = b.X - a.X;
            float yDiff = b.Y - a.Y;
            return (float)Math.Atan2(yDiff, xDiff);
        }

        public static void UpdateShotsToMoment(List<Shot> shots, long time)
        {
            foreach (var shot in shots)
            {
                shot.Update(time);
            }
        }

        public static bool CollideShots(List<Shot> shots, Player player)
        {
            foreach (var shot in shots)
            {
                if (shot.ShotFrame.Active)
                {
                    int distance = Distance(shot.ShotFrame, player);
                    if (distance < 64) return true;
                }
            }
            return false;
        }

        public static void ProcessMovementAndCollisions(QueuedMove first, Player clientSimPlayer, List<Shot> shots)
        {
            clientSimPlayer.X += first.X;
            clientSimPlayer.Y += first.Y;
            CheckCollisions(clientSimPlayer, shots, first.Tick);
        }

        private static void CheckCollisions(Player clientSimPlayer, List<Shot> shots, long tick)
        {
            Shared.UpdateShotsToMoment(shots, tick);
            bool hit = Shared.CollideShots(shots, clientSimPlayer);
            if (hit)
            {
                clientSimPlayer.Health -= 1;
            }
        }
    }
}
