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

        public static List<int> CollideShots(List<Shot> shots, Player player)
        {
            List<int> hits = new List<int>();
            foreach (var shot in shots)
            {
                if (shot.ShotFrame.Active)
                {
                    int distance = Distance(shot.ShotFrame, player);
                    if (distance < 64) hits.Add(shot.Id);
                }
            }
            return hits;
        }

        public static void ProcessMovementAndCollisions(QueuedMove move, Player clientSimPlayer, List<Shot> shots)
        {
            clientSimPlayer.X += move.X;
            clientSimPlayer.Y += move.Y;
            CheckCollisions(clientSimPlayer, shots, move.Tick);
            if (!move.Charging && clientSimPlayer.Charge > 0)
            {
                //attack
                clientSimPlayer.Charge = 0;
            }
            if (move.Charging)
            {
                clientSimPlayer.Charge++;
                if (clientSimPlayer.Charge > clientSimPlayer.MaxCharge) clientSimPlayer.Charge = clientSimPlayer.MaxCharge;
            }
        }

        private static void CheckCollisions(Player clientSimPlayer, List<Shot> shots, long tick)
        {
            Shared.UpdateShotsToMoment(shots, tick);
            List<int> hits = Shared.CollideShots(shots, clientSimPlayer);
            if (hits.Count > 0)
            {
                clientSimPlayer.Health -= 1;
            }
        }
    }
}
