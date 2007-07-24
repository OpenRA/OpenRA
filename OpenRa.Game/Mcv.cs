using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	class Mcv : Unit
	{
		public Mcv( int2 location, Player owner, Game game )
			: base( "mcv", location, owner, new float2( 12, 12 ), game )
		{
		}

		public void AcceptDeployOrder()
		{
			nextOrder = delegate( Game game, int t )
			{
				if( Turn( 12 ) )
					return;

				World world = game.world;
				world.AddFrameEndTask( delegate
				{
					world.Remove( this );
					world.Add( new ConstructionYard( fromCell - new int2( 1, 1 ), owner, game ) );
				} );
				currentOrder = null;
			};
		}

		public override IOrder Order( Game game, int2 xy )
		{
			if( ( fromCell == toCell || moveFraction == 0 ) && fromCell == xy )
				return new DeployMcvOrder( this );

			return base.Order( game, xy );
		}
	}
}
