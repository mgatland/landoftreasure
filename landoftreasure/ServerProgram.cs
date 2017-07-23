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
            server.Start(Port);

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("We got connection: {0}", peer.EndPoint); // Show peer ip
                NetDataWriter writer = new NetDataWriter();                 // Create writer class
                writer.Put((byte)1);
                writer.Put("Hello client!");                                // Put some string
                peer.Send(writer, SendOptions.ReliableOrdered);             // Send with reliability
                players.Add(new Player(peer.ConnectId));
            };

            while (!Console.KeyAvailable)
            {
                var peers = server.GetPeers();
                NetDataWriter writer = new NetDataWriter();
                foreach(var player in players) {
                    player.x += new Random().Next(5) - 2;
                    player.y += new Random().Next(5) - 2;
                    writer.Put((byte)0);
                    writer.Put(player.peerId);
                    writer.Put(player.x);
                    writer.Put(player.y);
					foreach (var p in peers)
					{
						p.Send(writer, SendOptions.Unreliable);
					}
                    writer.Reset();
                }
                Console.WriteLine("{0} clients", peers.Count());
                server.PollEvents();
                Thread.Sleep(15);
            }

            server.Stop();
        }
    }
}
