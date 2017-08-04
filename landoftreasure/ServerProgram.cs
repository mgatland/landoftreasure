using System;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Linq;
using System.Collections.Generic;
using lotshared;
using System.Diagnostics;

namespace landoftreasure
{
    class MainClass
    {
        private const int MaxClients = 20;
        private const int Port = 9050;
        private const string connectionKey = "landoftreasure";
        private const int simulationTickRate = 1000 / 30;
        private const int maximumLagAllowed = 1000;
        private const int playerReplayLag = maximumLagAllowed; //probably must be tuned per player based on their lag

        private List<NetPlayer> netPlayers;
        private List<Player> players;
        private List<Creature> creatures;
        List<Shot> shots;

        private Random random = new Random();
        private Stopwatch stopwatch = new Stopwatch();
        private long lastStep = 0;

        private long lastPing = 0;
        private int pingFrequency = 1000 * 10;

        public static void Main(string[] args)
        {
            new MainClass().Start();
        }

        private void Start()
        {
            Console.WriteLine("Starting game server…");

            stopwatch.Start();
            netPlayers = new List<NetPlayer>();
            players = new List<Player>();
            creatures = new List<Creature>();
            shots = new List<Shot>();

            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager server = new NetManager(listener, MaxClients, connectionKey);
            if (Packets.SimulateLatency)
            {
                server.SimulateLatency = true;
                server.SimulationMinLatency = Packets.SimulationMinLatency;
                server.SimulationMaxLatency = Packets.SimulationMaxLatency;
                server.SimulatePacketLoss = Packets.SimulatePacketLoss;
                server.SimulationPacketLossChance = Packets.SimulationPacketLossChance;
            }
            server.Start(Port);

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("We got connection: {0}", peer.EndPoint);
                var netPlayer = new NetPlayer(peer.ConnectId, peer);
                netPlayer.Player = new Player(NewPlayerId());
                netPlayer.ClientSimPlayer = new Player(NewPlayerId());
                netPlayers.Add(netPlayer);
                players.Add(netPlayer.Player);
                Console.WriteLine("{0} clients", server.GetPeers().Count());
				NetDataWriter writer = new NetDataWriter();
				writer.Put(Packets.WelcomeClient);
                writer.Put(netPlayer.Player.Id);
				writer.Put(lastStep);
                writer.Put(netPlayer.Player.X);
                writer.Put(netPlayer.Player.Y);
                peer.Send(writer, SendOptions.ReliableOrdered);
            };

            listener.NetworkReceiveEvent += (peer, reader) => 
            {
                byte packetType = reader.GetByte();
                if (packetType==Packets.Pong)
                {
                    var pingStart = reader.GetLong();
                    var lag = stopwatch.ElapsedMilliseconds - pingStart;
                    Console.WriteLine("Ping: " + lag);
                }
                if (packetType==Packets.ClientMovement) {
                    var player = netPlayers.Find(p => p.PeerId == peer.ConnectId);
                    if (player == null)
                    {
                        Console.WriteLine("Peer tried to move but has no associated NetPlayer");
                    }
                    else
                    {
                        //Console.Write("Getting moveticks ");
                        var count = reader.GetInt();
                        long tick = reader.GetLong();
                        for (var i = 0; i < count; i++) {
							tick += reader.GetInt(); //delta
                            sbyte x = reader.GetSByte();
							sbyte y = reader.GetSByte();
                            if (tick > player.LastAckedMove 
                            && !player.MoveQueueUnverified.Any(qm => qm.Tick == tick)
                            && !player.MoveQueueVerified.Any(qm => qm.Tick == tick)) {
								player.MoveQueueUnverified.Add(new QueuedMove(tick, x, y));
								//Console.WriteLine("Client Move: " + tick + " vs real time " + lastStep + " with " + count + " move snapshots");
								//Console.WriteLine("Move lag: " + (lastStep - tick));
							} // else dropping duplicate or out-of-order movement
							if (i == count - 1 && player.LastAckedMove < tick)
							{
                                player.LastAckedMove = tick;
								player.RecordLatency((int)(lastStep - tick));
							}
                        }
                        //Console.WriteLine();

                    }
                }
            };

            while (!Console.KeyAvailable)
            {
                long elapsed = stopwatch.ElapsedMilliseconds;
                while (elapsed > lastStep + simulationTickRate)
                {
                    Update(server);
                    lastStep = lastStep + simulationTickRate;
                }
                long postUpdateElapsed = stopwatch.ElapsedMilliseconds;
                int earlyAmount = (int)(lastStep + simulationTickRate - postUpdateElapsed);
                if (earlyAmount > 2)
                {
                    Thread.Sleep(earlyAmount - 2);
                }
            }

            server.Stop();
        }


        private void Update(NetManager server)
        {
            if (creatures.Count < 1)
            {
                SpawnCreature();
            }
            foreach (var creature in creatures)
            {
                creature.angle += 0.09f;
                if (creature.angle > Math.PI * 2) creature.angle -= (float)(Math.PI * 2);

                creature.X += (int)(Math.Sin(creature.angle) * 16);
                creature.Y += (int)(Math.Cos(creature.angle) * 16);

                creature.timer++;
                if (creature.timer >= 60)
                {
                    Player target = Shared.FindClosestPlayer(creature, players);
                    if (target != null)
                    {
                        float angle = Shared.AngleFromTo(creature, target);
                        creature.timer = 0;
                        SpawnShot(creature, angle);
                    }
                }
            }

            foreach(var p in netPlayers)
            {
                ProcessPlayerMovement(p);
            }

            //Remove old shots
            shots.RemoveAll(s => s.IsDead(lastStep, maximumLagAllowed));

            NetDataWriter writer = new NetDataWriter();

            SendSnapshotUpdates(writer);
            SendShotUpdates(writer);

            server.PollEvents();
        }

        private void ProcessPlayerMovement(NetPlayer p)
        {
            //TODO: if the player is too far behind, we insert fake movement

            while (p.MoveQueueUnverified.Count > 0)
            {
                var first = p.MoveQueueUnverified[0];
                p.MoveQueueUnverified.RemoveAt(0);
                //TODO: check for speed hacks
                Shared.ProcessMovementAndCollisions(first, p.ClientSimPlayer, shots);
                p.MoveQueueVerified.Add(first);
            }
            //Copy health from client version to shared version of player
            p.Player.Health = p.ClientSimPlayer.Health;

            //Publish any movement that is sufficiently old
            while (p.MoveQueueVerified.Count > 0)
            {
                int count = 0;
                var first = p.MoveQueueVerified[0];
                if (first.Tick < lastStep - playerReplayLag)
                {
                    p.Player.X += first.X;
                    p.Player.Y += first.Y;
                    p.MoveQueueVerified.RemoveAt(0);
                    count++;
                }
                else
                {
                    break;
                }
            }
        }

        private void SendSnapshotUpdates(NetDataWriter writer)
        {
            var sendPings = false;
            if (stopwatch.ElapsedMilliseconds > lastPing + pingFrequency)
            {
                sendPings = true;
                lastPing += pingFrequency;
            }
            foreach (var p in netPlayers)
            {
                if (sendPings)
                {
                    writer.Put(Packets.Ping);
                    //sending the time instead of an ID is a bit lazy, it means clients could lie about their ping.
                    writer.Put(stopwatch.ElapsedMilliseconds);
                    p.Peer.Send(writer, SendOptions.Unreliable);
                    writer.Reset();
                }

                Snapshot snapshot = new Snapshot();
                snapshot.Timestamp = lastStep;
                snapshot.LastAckedClientMove = p.LastAckedMove;
                foreach (var creature in creatures)
                {
                    snapshot.Creatures.Add(creature.Id, creature);
                }
                foreach (var player in players)
                {
                    snapshot.Players.Add(player.Id, player);
                }
                snapshot.Serialize(writer);
                p.Peer.Send(writer, SendOptions.Unreliable);
                writer.Reset();
            }
        }

        private void SendShotUpdates(NetDataWriter writer)
        {
            //Send shot updates
            foreach (var p in netPlayers)
            {
                //Which shots have changed since the last update?
                List<Shot> shotsToSend = new List<Shot>();
                foreach (var shot in shots)
                {
                    if (!p.ShotKnowledge.ContainsKey(shot.Id) || p.ShotKnowledge[shot.Id] < shot.DirtyTime)
                    {
                        p.ShotKnowledge[shot.Id] = shot.DirtyTime;
                        shotsToSend.Add(shot);
                    }
                }
                foreach (var shot in shotsToSend)
                {
                    writer.Put(Packets.Shot);
                    writer.Put(shot.Id);
                    writer.Put(shot.SpawnTime);
                    writer.Put(shot.X);
                    writer.Put(shot.Y);
                    writer.Put(shot.Angle);
                    p.Peer.Send(writer, SendOptions.ReliableOrdered);
                    writer.Reset();
                }
                if (shotsToSend.Count > 0)
                {
                    Debug.WriteLine("Sending client " + shotsToSend.Count + " new shots");
                }
            }
        }

        private void SpawnShot(Creature creature, float angle)
        {
            var shot = new Shot();
            shot.X = creature.X;
            shot.Y = creature.Y;
            shot.Angle = angle;
            shot.Id = NewShotId();
            shot.SpawnTime = lastStep;
            shots.Add(shot);
        }

        private void SpawnCreature()
        {
            var creature = new Creature();
            creature.X = 0 + random.Next(20);
            creature.Y = 0 + random.Next(20);
            creature.Id = NewCreatureId();
            creatures.Add(creature);
        }

        //TODO: handle these filling up
        private int lastShotId;
        private int NewShotId() {
            return lastShotId++;
        }
		private int lastCreatureId;
		private int NewCreatureId()
		{
			return lastCreatureId++;
		}
		private int lastPlayerId;
		private int NewPlayerId()
		{
			return lastPlayerId++;
		}
    }
}
