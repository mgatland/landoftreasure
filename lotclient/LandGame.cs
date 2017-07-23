using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiteNetLib;
using lotshared;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        List<Player> players;

        public LandGame()
        {
            var graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
            this.Window.Title = "Land of Treasure";
            SpriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Initialize()
        {
            players = new List<Player>();

            EventBasedNetListener listener = new EventBasedNetListener();
            client = new NetManager(listener, hostKey);
            client.Start();
            client.Connect(host, hostPort);
            listener.NetworkReceiveEvent += (fromPeer, dataReader) =>
            {
                byte packetType = dataReader.GetByte();
                if (packetType==0) {
                    long peerId = dataReader.GetLong();
                    int x = dataReader.GetInt();
                    int y = dataReader.GetInt();
                    var netPlayer = players.Find(p => p.peerId == peerId);
                    if (netPlayer == null) {
                        netPlayer = new Player(peerId);
                        players.Add(netPlayer);
                    }
                    netPlayer.x = x;
                    netPlayer.y = y;
                }
                if (packetType==1) {
                    Debug.WriteLine("We got: {0}", dataReader.GetString(100 /* max length of string */), "");    
                }
            };
            base.Initialize();
        }

        protected override void LoadContent()
        {
            Texture2D = Content.Load<Texture2D>("content/test.png");
            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            client.PollEvents();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Cornsilk);
            if (Texture2D == null) return;
            SpriteBatch.Begin();
            players.ForEach(p => SpriteBatch.Draw(Texture2D, new Vector2(p.x, p.y), Color.White));
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
