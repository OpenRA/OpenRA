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
		public Mcv( int2 location, int palette )
			: base( "mcv", location, palette, new float2( 12, 12 ) )
		{
		}

		public void AcceptDeployOrder()
		{
			nextOrder = delegate( Game game, double t )
			{
				if( Turn( 12 ) )
					return;

				World world = game.world;
				world.AddFrameEndTask( delegate
				{
					world.Remove( this );
					world.Add( new ConstructionYard( fromCell - new int2( 1, 1 ), palette ) );
					world.Add( new Refinery( fromCell - new int2( 1, -2 ), palette ) );

					world.myUnit = new Harvester( fromCell - new int2( 0, -4 ), palette );
					world.Add( (Actor)world.myUnit );
				} );
				currentOrder = null;
			};
		}

		public override IOrder Order( int2 xy )
		{
			if( ( fromCell == toCell || moveFraction == 0 ) && fromCell == xy )
				return new DeployMcvOrder( this );

			return base.Order( xy );
		}
	}
}
