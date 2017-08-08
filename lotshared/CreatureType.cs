using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lotshared
{
    public class CreatureType
    {
        private static Random random = new Random();

        private int chargeDistance = 300;
        private int desiredDistance = 150;
        private int speed = 8;
        private int dodgeTime = 16;
        private int RefireDelay = 30;

        public void Start(Creature creature)
        {
            creature.State = 1;
            creature.timer = -RefireDelay + (int)(random.Next(RefireDelay));
        }

        private void ChangeState(Creature creature, byte state, World world)
        {
            creature.State = state;
            creature.StateTimer = 0;
            if (creature.State == 1) //charge
            {
                Player target = Shared.FindClosestPlayer(creature, world.Players);
                if (target != null) creature.angle = Shared.AngleFromTo(creature, target);
            }
        }

        public void Update(Creature creature, World world)
        {
            creature.StateTimer++; //fixme: framerate dependent (not that the server framerate changes)
            Player target = Shared.FindClosestPlayer(creature, world.Players);
            int distance = (target != null) ? Shared.Distance(creature, target) : -1;
            if (creature.State == 1) //charge
            {
                if (distance < desiredDistance || target == null)
                {
                    ChangeState(creature, 2, world);
                }
                else
                {
                    creature.angle = Shared.AngleFromTo(creature, target);
                    MoveAtCurrentAngle(creature, speed);
                }
            } else if (creature.State == 2) //dodge
            {
                if (target != null && distance > chargeDistance)
                {
                    ChangeState(creature, 1, world);
                }
                else
                {
                    if (creature.StateTimer == 1)
                    {
                        creature.angle = (float)(random.NextDouble() * Math.PI * 2);
                    }
                    if (creature.StateTimer >= dodgeTime) creature.StateTimer = 0;
                    MoveAtCurrentAngle(creature, speed);
                }
            }

            //spin around in a big circle:
            //creature.angle += 0.09f;
            //if (creature.angle > Math.PI * 2) creature.angle -= (float)(Math.PI * 2);

            creature.timer++; //framerate dependent
            if (creature.timer >= RefireDelay)
            {
                if (target != null)
                {
                    float angle = Shared.AngleFromTo(creature, target);
                    creature.timer = 0;
                    world.SpawnShot(creature, angle);
                }
            }
        }

        private static void MoveAtCurrentAngle(Creature creature, int speed)
        {
            creature.X += (int)(Math.Cos(creature.angle) * speed);
            creature.Y += (int)(Math.Sin(creature.angle) * speed);
        }
    }
}
