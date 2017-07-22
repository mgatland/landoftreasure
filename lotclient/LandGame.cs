using System;
using System.Diagnostics;
using LiteNetLib;
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

        public LandGame()
        {
            var graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
            this.Window.Title = "Land of Treasure";
        }

        protected override void Initialize()
        {

            EventBasedNetListener listener = new EventBasedNetListener();
            client = new NetManager(listener, hostKey);
            client.Start();
            client.Connect(host, hostPort);
            listener.NetworkReceiveEvent += (fromPeer, dataReader) =>
            {
                Debug.WriteLine("We got: {0}", dataReader.GetString(100 /* max length of string */), "");
            };
        }

        protected override void Update(GameTime gameTime)
        {
            client.PollEvents();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Cornsilk);

            base.Draw(gameTime);
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            client.Stop();
            base.OnExiting(sender, args);
        }
    }
}
