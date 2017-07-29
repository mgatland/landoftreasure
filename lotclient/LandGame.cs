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

        private const int desiredExtraDelay = 100;
        private int extraDelay = desiredExtraDelay;

        NetManager client;

        SpriteBatch SpriteBatch;
        Texture2D Texture2D;
        Texture2D creatureTexture;
        Texture2D shotTexture;

        int Id;
        List<Player> players;
        List<Snapshot> snapshots = new List<Snapshot>();
        List<Creature> creatures;
        List<Shot> shots;
        Player player;

        private int screenWidth = 1024;
        private int screenHeight = 768;

        private int cameraX;
        private int cameraY;

        private Stopwatch stopwatch = new Stopwatch();
        private long serverStartTick;

        public LandGame()
        {
            var graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = screenWidth;
            graphics.PreferredBackBufferHeight = screenHeight;
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
            if (Packets.SimulateLatency)
            {
                client.SimulateLatency = true;
                client.SimulationMinLatency = Packets.SimulationMinLatency;
                client.SimulationMaxLatency = Packets.SimulationMaxLatency;
                client.SimulatePacketLoss = true;
                client.SimulationPacketLossChance = Packets.SimulationPacketLossChance;
            }
            client.Start();
            client.Connect(host, hostPort);
            listener.NetworkReceiveEvent += (fromPeer, dataReader) =>
            {
                byte packetType = dataReader.GetByte();
                if (packetType == Packets.WelcomeClient)
                {
                    this.Id = dataReader.GetInt();
                    serverStartTick = dataReader.GetLong();
                    stopwatch.Restart();
                }
                if (packetType == Packets.Snapshot)
				{
                    snapshots.Add(Snapshot.Deserialize(dataReader));

				}
				if (packetType == Packets.Shot)
				{
					int id = dataReader.GetInt();
                    var shot = shots.Find(p => p.Id == id);
					if (shot == null)
					{
                        Debug.WriteLine("Adding shot {0}", id);
						shot = new Shot();
						shot.Id = id;
						shots.Add(shot);
					}
                    shot.SpawnTime = dataReader.GetLong();
					shot.X = dataReader.GetInt();
					shot.Y = dataReader.GetInt();
                    shot.Angle = dataReader.GetFloat();
                }
                if (packetType==Packets.Message) {
                    Debug.WriteLine("We got: {0}", dataReader.GetString(100 /* max length of string */), "");    
                }
            };
            base.Initialize();
        }


        //servertick is server frame we are trying to display.
        //It should be 1 or 2 frames behind the latest server frame we have recieved
        private long calculateServerTick()
        {
            return serverStartTick + stopwatch.ElapsedMilliseconds + extraDelay;
        }

        protected override void LoadContent()
        {
            Texture2D = Content.Load<Texture2D>("content/test.png");
            creatureTexture = Content.Load<Texture2D>("content/creature.png");
            shotTexture = Content.Load<Texture2D>("content/shot.png");
            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            long serverTick = calculateServerTick();

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
                writer.Put(serverTick);
                writer.Put(dX);
                writer.Put(dY);
                client.GetFirstPeer().Send(writer, SendOptions.ReliableOrdered);
                client.PollEvents();
            }

            if (player == null)
            {
                player = players.Find(p => p.Id == Id);
            }

            //blend between two snapshots
            var nextSnapIndex = snapshots.FindIndex(shot => shot.Timestamp >= serverTick);
            if (nextSnapIndex >= 0)
            {
                var lastSnapIndex = nextSnapIndex - 1;
                if (lastSnapIndex >= 0)
                {
                    var lastSnap = snapshots[lastSnapIndex];
                    var nextSnap = snapshots[nextSnapIndex];
                    int timeSpan = (int)(nextSnap.Timestamp - lastSnap.Timestamp);
                    float newAmount = (serverTick - lastSnap.Timestamp) / 1f / timeSpan;
                    float oldAmount = 1f - newAmount;
                    foreach (var c in lastSnap.Creatures.Values)
                    {
                        var creature = creatures.Find(p => p.Id == c.Id);
                        if (creature == null)
                        {
                            Debug.WriteLine("Adding creature {0}", c.Id);
                            creature = new Creature();
                            creature.Id = c.Id;
                            creatures.Add(creature);
                        }
                        var cNext = nextSnap.Creatures.ContainsKey(c.Id) ? nextSnap.Creatures[c.Id] : c;
                        creature.X = (int)Math.Round(c.X * oldAmount + cNext.X * newAmount);
                        creature.Y = (int)Math.Round(c.Y * oldAmount + cNext.Y * newAmount);
                    }
                    foreach (var p in lastSnap.Players.Values)
                    {
                        var player = players.Find(o => o.Id == p.Id);
                        if (player == null)
                        {
                            Debug.WriteLine("Adding player {0}", p.Id);
                            player = new Player(p.Id);
                            players.Add(player);
                        }
                        var cNext = nextSnap.Players.ContainsKey(p.Id) ? nextSnap.Players[p.Id] : p;
                        player.X = (int)Math.Round(p.X * oldAmount + cNext.X * newAmount);
                        player.Y = (int)Math.Round(p.Y * oldAmount + cNext.Y * newAmount);
                    }

                    //Remove stale snapshots
                    snapshots.RemoveRange(0, lastSnapIndex);
                }
            }
            if ((snapshots.Count > 2 && nextSnapIndex < 0) || (snapshots.Count < 3 && stopwatch.ElapsedMilliseconds > 1000))
            {
                //If there are snaps but we're not picking one, we must think we're in the future.
                //OR: if there are not many snapshots, that indicates we are too close to the present
                //(except at the start of the game)
                serverStartTick -= 10;
                Console.WriteLine("We got ahead of the server, wait 10ms");
            }
            if (snapshots.Count > 10 && nextSnapIndex >= 0)
            {
                Console.WriteLine("We got behind the server, jump ahead 10ms");
                serverStartTick += 10;
            }

            if (player != null)
            {
                cameraX = player.X - screenWidth / 2;
                cameraY = player.Y - screenHeight / 2;
            }

            Shared.UpdateShots(shots, serverTick);
            shots.RemoveAll(s => s.IsDead(serverTick, 0));

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Cornsilk);
            if (Texture2D == null) return;
            SpriteBatch.Begin();
            players.ForEach(p => DrawSprite(Texture2D, p.X, p.Y));
            creatures.ForEach(p => DrawSprite(creatureTexture, p.X, p.Y));
            shots.ForEach(p => { if (p.ShotFrame.Active) SpriteBatch.Draw(shotTexture, new Vector2(p.ShotFrame.X - cameraX, p.ShotFrame.Y - cameraY), null, Color.White, p.Angle, new Vector2(32, 8), new Vector2(1, 1), SpriteEffects.None, 0f); });
            SpriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawSprite(Texture2D texture, int x, int y)
        {
            SpriteBatch.Draw(texture, new Vector2(x - cameraX, y - cameraY), null, Color.White, 0f, new Vector2(texture.Width/2,texture.Height/2), 1f, SpriteEffects.None, 0f);
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            client.Stop();
            base.OnExiting(sender, args);
        }
    }
}
