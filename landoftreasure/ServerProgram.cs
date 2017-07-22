using System;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Linq;

namespace landoftreasure
{
    class MainClass
    {
        private const int MaxClients = 20;
        private const int Port = 9050;
        private const string connectionKey = "landoftreasure";

        public static void Main(string[] args)
        {
            Console.WriteLine("Starting game server…");

            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager server = new NetManager(listener, MaxClients, connectionKey);
            server.Start(Port);

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine("We got connection: {0}", peer.EndPoint); // Show peer ip
                            NetDataWriter writer = new NetDataWriter();                 // Create writer class
                            writer.Put("Hello client!");                                // Put some string
                            peer.Send(writer, SendOptions.ReliableOrdered);             // Send with reliability
                        };

            while (!Console.KeyAvailable)
            {
                var peers = server.GetPeers();
                NetDataWriter writer = new NetDataWriter();
				writer.Put("hi");
				foreach (var p in peers) {
                    p.Send(writer, SendOptions.Unreliable);
                }
                Console.WriteLine("{0} clients", peers.Count());
                server.PollEvents();
                Thread.Sleep(15);
            }

            server.Stop();
        }
    }
}
