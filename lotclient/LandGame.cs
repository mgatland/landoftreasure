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
        private const int maxQueuedMovestoSend = 40;
        private int extraDelay = desiredExtraDelay;

        NetManager client;

        SpriteBatch SpriteBatch;
        Texture2D ServerPosMarkerTexture;
        Texture2D PlayerTexture;
        Texture2D creatureTexture;
        Texture2D shotTexture;
        Texture2D ringTexture;

        Matrix viewMatrix;
        Matrix projectionMatrix;
        BasicEffect basicEffect;
        GraphicsDeviceManager graphics;

        int Id;
        List<QueuedMove> queuedMoves = new List<QueuedMove>();
        List<int> previousHits = new List<int>();
        private long lastSendTime;
        private const int clientSendFrequency = 1000 / 20;
        private long LastAckedClientMove; //this variable isn't actually used, maybe nice for debugging

        List<Player> players;
        List<Snapshot> snapshots = new List<Snapshot>();
        private long latestSnapshotTimestamp = -1;
        List<Creature> creatures;
        List<Shot> shots;
        Player player;

        private int screenWidth = 1024;
        private int screenHeight = 768;

        private int cameraX;
        private int cameraY;

        private Stopwatch stopwatch = new Stopwatch();
        private long serverStartTick;

        private int maxAttackRange = 800;

        public LandGame()
        {
            graphics = new GraphicsDeviceManager(this);
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
            InitializeTransform();
            InitializeEffect();

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
                client.SimulatePacketLoss = Packets.SimulatePacketLoss;
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
                    player = new Player(-1);
                    player.X = dataReader.GetInt();
                    player.Y = dataReader.GetInt();
                    stopwatch.Restart();
                }
                if (packetType == Packets.Ping)
                {
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put(Packets.Pong);
                    writer.Put(dataReader.GetLong());
                    client.GetFirstPeer().Send(writer, SendOptions.Unreliable);
                }
                if (packetType == Packets.Snapshot)
                {
                    var snapshot = Snapshot.Deserialize(dataReader);
                    //check it's not a duplicate, or stale
                    if (snapshot.Timestamp > latestSnapshotTimestamp)
                    {
                        latestSnapshotTimestamp = snapshot.Timestamp;
                        snapshots.Add(snapshot);
                        //immediately process the ack
                        ProcessClientMovementAck(snapshot.LastAckedClientMove);
                    }
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
                if (packetType == Packets.Message)
                {
                    Debug.WriteLine("We got: {0}", dataReader.GetString(100 /* max length of string */), "");
                }
            };
            base.Initialize();
        }


        private void InitializeTransform()
        {

            viewMatrix = Matrix.CreateLookAt(
                new Vector3(0, 0, 1),
                Vector3.Zero,
                Vector3.Up
                );

            projectionMatrix = Matrix.CreateOrthographicOffCenter(
                0,
                (float)graphics.GraphicsDevice.Viewport.Width,
                (float)graphics.GraphicsDevice.Viewport.Height,
                0,
                1.0f, 100.0f);
        }

        private void InitializeEffect()
        {
            basicEffect = new BasicEffect(graphics.GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = viewMatrix;
            basicEffect.Projection = projectionMatrix;

        }

        private void ProcessClientMovementAck(long lastAckedClientMove)
        {
            int removedCount = queuedMoves.RemoveAll(qm => qm.Tick <= lastAckedClientMove);
            LastAckedClientMove = lastAckedClientMove;
            //Console.WriteLine("Server acked " + removedCount + " leaving " + queuedMoves.Count + " unacked");
        }


        //servertick is server frame we are trying to display.
        //It should be 1 or 2 frames behind the latest server frame we have recieved
        private long calculateServerTick()
        {
            return serverStartTick + stopwatch.ElapsedMilliseconds + extraDelay;
        }

        protected override void LoadContent()
        {
            ServerPosMarkerTexture = Content.Load<Texture2D>("content/serverPosMarker.png");
            PlayerTexture = Content.Load<Texture2D>("content/player.png");
            creatureTexture = Content.Load<Texture2D>("content/creature.png");
            shotTexture = Content.Load<Texture2D>("content/shot.png");
            ringTexture = Content.Load<Texture2D>("content/ring.png");
            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            long serverTick = calculateServerTick(); //the time we see on screen, might jump around due to latency

            KeyboardState state = Keyboard.GetState();
            if (state.IsKeyDown(Keys.Escape))
                Exit();

            if (player != null)
            {
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
                var isCharging = state.IsKeyDown(Keys.Space);
                ProcessLocalMovement(serverTick, dX, dY, isCharging);
                SendMovementToServer();
            }
            client.PollEvents();

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
                        player.Health = p.Health;
                        player.MaxHealth = p.MaxHealth;
                        player.Charge= p.Charge;
                        player.MaxCharge = p.MaxCharge;
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

            Shared.UpdateShotsToMoment(shots, serverTick);
            shots.RemoveAll(s => s.IsDead(serverTick, 0));

            base.Update(gameTime);
        }

        private void ProcessLocalMovement(long serverTick, sbyte dX, sbyte dY, bool charging)
        {
            var moveTick = serverTick;
            if (queuedMoves.Count > 0 && serverTick <= queuedMoves[queuedMoves.Count - 1].Tick)
            {
                moveTick = queuedMoves[queuedMoves.Count - 1].Tick + 1;
                Console.WriteLine("oops, server tick went backwards, we'll pretend it went forward");
            }
            var newMove = new QueuedMove(moveTick, dX, dY, charging);
            Shared.ProcessMovementAndCollisions(newMove, player, shots, previousHits);
            queuedMoves.Add(newMove);
        }

        private void SendMovementToServer()
        {
            long localTick = stopwatch.ElapsedMilliseconds;
            if (localTick > lastSendTime + clientSendFrequency)
            {
                lastSendTime += clientSendFrequency;
                //TODO: if we are too far behind, just jump up to the present instead of flooding the server
                if (client.GetFirstPeer() != null)
                {
                    NetDataWriter writer = new NetDataWriter();
                    writer.Put(Packets.ClientMovement);
                    var count = Math.Min(queuedMoves.Count, maxQueuedMovestoSend);
                    writer.Put(count);
                    long tick = count > 0 ? queuedMoves[0].Tick : 0;
                    writer.Put(tick);
                    //Console.Write("Sending ");
                    for (var i = 0; i < count; i++)
                    {
                        var qm = queuedMoves[i];
                        writer.Put((int)(qm.Tick - tick)); //delta
                        tick = qm.Tick;
                        writer.Put((sbyte)qm.X);
                        writer.Put((sbyte)qm.Y);
                        writer.Put(qm.Charging);
                        //Console.Write(qm.Tick + ",");
                    }
                    //Console.WriteLine();
                    client.GetFirstPeer().Send(writer, SendOptions.Unreliable);
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Cornsilk);
            if (PlayerTexture == null) return;
            SpriteBatch.Begin();
            players.ForEach(p => DrawPlayer(p));
            if (player != null) DrawPlayer(player);
            creatures.ForEach(p => DrawSprite(creatureTexture, p.X, p.Y));
            shots.ForEach(p => { if (p.ShotFrame.Active) SpriteBatch.Draw(shotTexture, new Vector2(p.ShotFrame.X - cameraX, p.ShotFrame.Y - cameraY), null, Color.White, p.Angle, new Vector2(32, 8), new Vector2(1, 1), SpriteEffects.None, 0f); });

            if (player != null) DrawAttackRing();

            SpriteBatch.End();

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
            RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                players.ForEach(p => DrawPlayerBars(p));
                if (player != null) DrawPlayerBars(player);
            }

            base.Draw(gameTime);
        }

        private void DrawAttackRing()
        {
            var x = player.X;
            var y = player.Y;
            var attackRange = 1f * maxAttackRange * player.Charge / player.MaxCharge;
            var scale = attackRange *1f / ringTexture.Width;
            SpriteBatch.Draw(ringTexture, new Vector2(x - cameraX, y - cameraY), null, Color.White, 0f, new Vector2(ringTexture.Width / 2, ringTexture.Height / 2), scale, SpriteEffects.None, 0f);
        }

        private void DrawPlayer(Player p)
        {
            if (p.Id == this.Id)
            {
                DrawSprite(ServerPosMarkerTexture, p.X, p.Y);
            }
            else
            {
                DrawSprite(PlayerTexture, p.X, p.Y);
            }
        }

        private void DrawPlayerBars(Player p)
        {
            var color = (p.Id == this.Id) ? Color.LightGreen : Color.Green;
            DrawBar(p.Health, p.MaxHealth, p.X, p.Y, color);
            var color2 = (p.Id == this.Id) ? Color.LightBlue : Color.Blue;
            DrawBar(p.Charge, p.MaxCharge, p.X, p.Y + barHeight, color);
        }

        private const int barHeight = 8;
        private void DrawBar(int current, int max, int X, int Y, Color color)
        {
            VertexPositionColor[] vertexData = new VertexPositionColor[4];
            Vector3 topLeft = new Vector3(X - 32 - cameraX, Y - cameraY, 0f);
            int width = 64 * current / max;
            Vector3 topRight = new Vector3(topLeft.X + width, Y - cameraY, 0f);
            Vector3 bottomLeft = new Vector3(X - 32 - cameraX, Y + barHeight - cameraY, 0f);
            Vector3 bottomRight = new Vector3(bottomLeft.X + width, Y + barHeight - cameraY, 0f);
            vertexData[0] = new VertexPositionColor(topLeft, color);
            vertexData[1] = new VertexPositionColor(topRight, color);
            vertexData[2] = new VertexPositionColor(bottomLeft, color);
            vertexData[3] = new VertexPositionColor(bottomRight, color);
            GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertexData, 0, 2);
        }

        private void DrawSprite(Texture2D texture, int x, int y)
        {
            SpriteBatch.Draw(texture, new Vector2(x - cameraX, y - cameraY), null, Color.White, 0f, new Vector2(texture.Width / 2, texture.Height), 1f, SpriteEffects.None, 0f);
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            client.Stop();
            base.OnExiting(sender, args);
        }
    }
}
