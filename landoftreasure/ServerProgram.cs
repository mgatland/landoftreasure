using System;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Linq;
using System.Collections.Generic;
using lotshared;

namespace landoftreasure
{
    class MainClass
    {
        private const int MaxClients = 20;
        private const int Port = 9050;
        private const string connectionKey = "landoftreasure";
        private const int frameDelay = 1000 / 30;

        private List<Player> players;
        private List<Creature> creatures;
        List<Shot> shots;

        private Random random = new Random();

        public static void Main(string[] args)
        {
            new MainClass().Start();
        }

        private void Start()
        {
            Console.WriteLine("Starting game server…");

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
                Console.WriteLine("We got connection: {0}", peer.EndPoint); // Show peer ip
                NetDataWriter writer = new NetDataWriter();                 // Create writer class
                writer.Put(Packets.Message);
                writer.Put("Hello client!");                                // Put some string
                peer.Send(writer, SendOptions.ReliableOrdered);             // Send with reliability
                players.Add(new Player(peer.ConnectId));
                Console.WriteLine("{0} clients", server.GetPeers().Count());
            };

            listener.NetworkReceiveEvent += (peer, reader) => 
            {
                byte packetType = reader.GetByte();
                if (packetType==Packets.ClientMovement) {
                    var player = players.Find(p => p.PeerId == peer.ConnectId);
                    sbyte x = reader.GetSByte();
                    sbyte y = reader.GetSByte();
                    //todo: cheap prevention
                    player.X += x;
                    player.Y += y;
                }
            };

            while (!Console.KeyAvailable)
            {
                Update(server);
            }

            server.Stop();
        }

        private void Update(NetManager server)
        {
            var peers = server.GetPeers();
            NetDataWriter writer = new NetDataWriter();
            foreach (var player in players)
            {
                writer.Put(Packets.PlayerPos);
                writer.Put(player.PeerId);
                writer.Put(player.X);
                writer.Put(player.Y);
                foreach (var p in peers)
                {
                    p.Send(writer, SendOptions.ReliableOrdered);
                }
                writer.Reset();
            }

            if (creatures.Count < 5)
            {
                SpawnCreature();
            }
            foreach (var creature in creatures)
            {
                creature.angle += 0.01f;
                if (creature.angle > Math.PI * 2) creature.angle -= (float)(Math.PI * 2);

                creature.X += (int)(Math.Sin(creature.angle) * 4);
                creature.Y += (int)(Math.Cos(creature.angle) * 4);

                creature.timer++;
                if (creature.timer >= 10) {
                    creature.timer = 0;
                    SpawnShot(creature);
                }
            }

            foreach(var shot in shots) {
                shot.Y++;
            }
            //delete some shots
            shots = shots.Where(s => s.Y < 600).ToList();

            //network
            foreach(var creature in creatures)
            {
				writer.Put(Packets.Creature);
				writer.Put(creature.Id);
				writer.Put(creature.X);
				writer.Put(creature.Y);
				foreach (var p in peers)
				{
					p.Send(writer, SendOptions.ReliableOrdered);
				}
				writer.Reset();
            }
			foreach (var shot in shots)
			{
				writer.Put(Packets.Shot);
				writer.Put(shot.Id);
				writer.Put(shot.X);
				writer.Put(shot.Y);
				foreach (var p in peers)
				{
					p.Send(writer, SendOptions.ReliableOrdered);
				}
				writer.Reset();
			}

            server.PollEvents();
            Thread.Sleep(frameDelay);
        }

        private void SpawnShot(Creature creature)
        {
            var shot = new Shot();
            shot.X = creature.X;
            shot.Y = creature.Y;
            shot.Id = NewId();
            shots.Add(shot);
        }

        private void SpawnCreature()
        {
            var creature = new Creature();
            creature.X = 200 + random.Next(200);
            creature.Y = 200 + random.Next(200);
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
