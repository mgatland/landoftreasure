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

        private List<NetPlayer> netPlayers;
        private List<Player> players;
        private List<Creature> creatures;
        List<Shot> shots;

        private Random random = new Random();
        private Stopwatch stopwatch = new Stopwatch();
        private long lastStep = 0;

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
                server.SimulatePacketLoss = true;
                server.SimulationPacketLossChance = Packets.SimulationPacketLossChance;
            }
            server.Start(Port);

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("We got connection: {0}", peer.EndPoint);
                NetDataWriter writer = new NetDataWriter();
                writer.Put(Packets.WelcomeClient);
                writer.Put(peer.ConnectId);
                writer.Put(lastStep);
                peer.Send(writer, SendOptions.ReliableOrdered);
                var netPlayer = new NetPlayer(peer.ConnectId, peer);
                netPlayer.Player = new Player(peer.ConnectId);
                netPlayers.Add(netPlayer);
                players.Add(netPlayer.Player);
                Console.WriteLine("{0} clients", server.GetPeers().Count());
            };

            listener.NetworkReceiveEvent += (peer, reader) => 
            {
                byte packetType = reader.GetByte();
                if (packetType==Packets.ClientMovement) {
                    var player = netPlayers.Find(p => p.PeerId == peer.ConnectId);
                    sbyte x = reader.GetSByte();
                    sbyte y = reader.GetSByte();
                    //todo: cheat prevention
                    player.Player.X += x;
                    player.Player.Y += y;
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
            var peers = server.GetPeers();
            NetDataWriter writer = new NetDataWriter();
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

            //Remove old shots
            shots.RemoveAll(s => s.IsDead(lastStep, maximumLagAllowed));

            SendSnapshotUpdates(writer);
            SendShotUpdates(writer);

            server.PollEvents();
        }

        private void SendSnapshotUpdates(NetDataWriter writer)
        {
            foreach (var p in netPlayers)
            {
                Snapshot snapshot = new Snapshot();
                snapshot.Timestamp = lastStep;
                foreach (var creature in creatures)
                {
                    snapshot.Creatures.Add(creature.Id, creature);
                }
                foreach (var player in players)
                {
                    snapshot.Players.Add(player.Id, player);
                }
                snapshot.Serialize(writer);
            p.Peer.Send(writer, SendOptions.Sequenced);
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
            shot.Id = NewId();
            shot.SpawnTime = lastStep;
            shots.Add(shot);
        }

        private void SpawnCreature()
        {
            var creature = new Creature();
            creature.X = 0 + random.Next(20);
            creature.Y = 0 + random.Next(20);
            creature.Id = NewId();
            creatures.Add(creature);
        }

        //TODO: handle this filling up
        private int lastId;
        private int NewId() {
            return lastId++;
        }
    }
}
