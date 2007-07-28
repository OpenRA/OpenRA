using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using BluntDirectX.Direct3D;
using System.Drawing;

namespace OpenRa.Game
{
	class Refinery : Building
	{
		public Refinery( int2 location, Player owner, Game game )
			: base( "proc", location, owner, game )
		{
			animation.PlayThen("make", delegate
			{
				animation.PlayRepeating("idle");

				game.world.AddFrameEndTask(delegate
				{
					Unit harvester = new Unit( "harv", location + new int2( 1, 2 ), owner, game );
					harvester.facing = 8;
					game.world.Add(harvester);
					game.world.orderGenerator = harvester;
				});
			});
		}
	}
}
