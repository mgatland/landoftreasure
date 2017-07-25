using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiteNetLib;
using LiteNetLib.Utils;
using lotshared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace lotclient
{
    public class LandGame : Game
    {
		private const string host = "localhost";
		private const int hostPort = 9050;
		private const string hostKey = "landoftreasure";

        NetManager client;

        SpriteBatch SpriteBatch;
        Texture2D Texture2D;
        Texture2D creatureTexture;

        List<Player> players;
        List<Creature> creatures;
        List<Shot> shots;

        public LandGame()
        {
            var graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
            graphics.IsFullScreen = false;
            graphics.SynchronizeWithVerticalRetrace = true;
            graphics.ApplyChanges();
            this.Window.Title = "Land of Treasure";
            SpriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Initialize()
        {
            players = new List<Player>();
            creatures = new List<Creature>();
            shots = new List<Shot>();

            EventBasedNetListener listener = new EventBasedNetListener();
            client = new NetManager(listener, hostKey);
			client.SimulateLatency = true;
			client.SimulationMinLatency = Packets.SimulationMinLatency;
            client.SimulationMaxLatency = Packets.SimulationMaxLatency;
			//client.SimulatePacketLoss = true;
			client.SimulationPacketLossChance = Packets.SimulationPacketLossChance;
            client.Start();
            client.Connect(host, hostPort);
            listener.NetworkReceiveEvent += (fromPeer, dataReader) =>
            {
                byte packetType = dataReader.GetByte();
                if (packetType == Packets.PlayerPos) {
                    long peerId = dataReader.GetLong();
                    int x = dataReader.GetInt();
                    int y = dataReader.GetInt();
                    var netPlayer = players.Find(p => p.PeerId == peerId);
                    if (netPlayer == null) {
                        Debug.WriteLine("Adding player {0}", peerId);
                        netPlayer = new Player(peerId);
                        players.Add(netPlayer);
                    }
                    netPlayer.X = x;
                    netPlayer.Y = y;
                }
                if (packetType == Packets.Creature)
				{
					int id = dataReader.GetInt();
					int x = dataReader.GetInt();
					int y = dataReader.GetInt();
					var creature = creatures.Find(p => p.Id == id);
					if (creature == null)
					{
                        Debug.WriteLine("Adding creature {0}", id);
						creature = new Creature();
                        creature.Id = id;
						creatures.Add(creature);
					}
					creature.X = x;
					creature.Y = y;
				}
				if (packetType == Packets.Shot)
				{
					int id = dataReader.GetInt();
					int x = dataReader.GetInt();
					int y = dataReader.GetInt();
                    var shot = shots.Find(p => p.Id == id);
					if (shot == null)
					{
                        Debug.WriteLine("Adding shot {0}", id);
						shot = new Shot();
						shot.Id = id;
						shots.Add(shot);
					}
					shot.X = x;
					shot.Y = y;
				}
                if (packetType==Packets.Message) {
                    Debug.WriteLine("We got: {0}", dataReader.GetString(100 /* max length of string */), "");    
                }
            };
            base.Initialize();
        }

        protected override void LoadContent()
        {
            Texture2D = Content.Load<Texture2D>("content/test.png");
            creatureTexture = Content.Load<Texture2D>("content/creature.png");
            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
			KeyboardState state = Keyboard.GetState();
			if (state.IsKeyDown(Keys.Escape))
				Exit();

            sbyte dX = 0;
            sbyte dY = 0;
			if (state.IsKeyDown(Keys.Right))
				dX += 4;
			if (state.IsKeyDown(Keys.Left))
				dX -= 4;
			if (state.IsKeyDown(Keys.Up))
				dY -= 4;
			if (state.IsKeyDown(Keys.Down))
				dY += 4;

            if (client.GetFirstPeer() != null)
            {
                NetDataWriter writer = new NetDataWriter();
                writer.Put(Packets.ClientMovement);
                writer.Put(dX);
                writer.Put(dY);
                client.GetFirstPeer().Send(writer, SendOptions.ReliableOrdered);
                client.PollEvents();
            }
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Cornsilk);
            if (Texture2D == null) return;
            SpriteBatch.Begin();
            players.ForEach(p => SpriteBatch.Draw(Texture2D, new Vector2(p.X, p.Y), Color.White));
            creatures.ForEach(p => SpriteBatch.Draw(creatureTexture, new Vector2(p.X, p.Y), Color.White));
            shots.ForEach(p => SpriteBatch.Draw(creatureTexture, new Vector2(p.X, p.Y), Color.Red));
            SpriteBatch.End();
            base.Draw(gameTime);
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            client.Stop();
            base.OnExiting(sender, args);
        }
    }
}
