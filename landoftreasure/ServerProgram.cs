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

        private List<Player> players;

        public static void Main(string[] args)
        {
            new MainClass().Start();
        }

        private void Start()
        {
            Console.WriteLine("Starting game server…");

            players = new List<Player>();

            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager server = new NetManager(listener, MaxClients, connectionKey);
            server.SimulateLatency = true;
            server.SimulationMinLatency = Packets.SimulationMinLatency;
            server.SimulationMaxLatency = Packets.SimulationMaxLatency;
            server.SimulatePacketLoss = true;
            server.SimulationPacketLossChance = Packets.SimulationPacketLossChance;
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
                    var player = players.Find(p => p.peerId == peer.ConnectId);
                    sbyte x = reader.GetSByte();
                    sbyte y = reader.GetSByte();
                    //todo: cheap prevention
                    player.x += x;
                    player.y += y;
                }
            };

            while (!Console.KeyAvailable)
            {
                var peers = server.GetPeers();
                NetDataWriter writer = new NetDataWriter();
                foreach(var player in players) {
                    writer.Put(Packets.PlayerPos);
                    writer.Put(player.peerId);
                    writer.Put(player.x);
                    writer.Put(player.y);
					foreach (var p in peers)
					{
                        p.Send(writer, SendOptions.ReliableOrdered);
					}
                    writer.Reset();
                }
                server.PollEvents();
                Thread.Sleep(15);
            }

            server.Stop();
        }
    }
}
