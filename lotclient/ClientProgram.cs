using System;
using System.Threading;
using LiteNetLib;
using Microsoft.Xna.Framework;

namespace lotclient
{
    class MainClass: Game
    {
		public static void Main(string[] args)
		{
			using (var game = new LandGame())
				game.Run();
		}
    }
}
