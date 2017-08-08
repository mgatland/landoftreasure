using System;
using System.Collections.Generic;
using System.Linq;

namespace lotshared
{
    public static class Shared
    {
        public static int MaxPlayerAttackRange = 200;

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

        private static List<int> CollideShots(List<Shot> shots, Player player)
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

        public static void ProcessMovementAndCollisions(QueuedMove move, Player clientSimPlayer, List<Shot> shots, List<int> previousHits)
        {
            clientSimPlayer.X += move.X;
            clientSimPlayer.Y += move.Y;
            CheckCollisions(clientSimPlayer, shots, previousHits, move.Tick);
        }

        public static AttackState ProcessPlayerAttackCharge(QueuedMove move, Player clientSimPlayer)
        {
            AttackState result = null;
            if (!move.Charging && clientSimPlayer.Charge > 0)
            {
                //attack
                result = new AttackState(Shared.MaxPlayerAttackRange*clientSimPlayer.Charge/clientSimPlayer.MaxCharge);
                clientSimPlayer.Charge = 0;
            }
            if (move.Charging)
            {
                clientSimPlayer.Charge++;
                if (clientSimPlayer.Charge > clientSimPlayer.MaxCharge) clientSimPlayer.Charge = clientSimPlayer.MaxCharge;
            }
            return result;
        }

        private static void CheckCollisions(Player clientSimPlayer, List<Shot> shots, List<int> previousHits, long tick)
        {
            Shared.UpdateShotsToMoment(shots, tick);
            List<int> hits = Shared.CollideShots(shots, clientSimPlayer);
            //hits that were not on our previous hits list
            foreach(var hit in hits.Except(previousHits)) {
                clientSimPlayer.Health -= 10;
            }
            previousHits.Clear();
            previousHits.AddRange(hits);
        }
    }
}
