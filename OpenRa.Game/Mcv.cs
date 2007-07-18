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
		static Range<int> mcvRange = UnitSheetBuilder.GetUnit( "mcv" );

		public Mcv( int2 location, int palette )
			: base( location, palette )
		{
		}

		public void AcceptDeployOrder()
		{
			nextOrder = delegate( World world, double t )
			{
				int desiredFacing = 12;
				if( facing != desiredFacing )
					Turn( desiredFacing );
				else
				{
					world.AddFrameEndTask( delegate
					{
						world.Remove( this );
						world.Add( new ConstructionYard( fromCell - new int2( 1, 1 ), palette ) );
						world.Add( new Refinery( fromCell - new int2( 1, -2 ), palette ) );
					} );
					currentOrder = null;
				}
			};
		}

		public override Sprite[] CurrentImages
		{
			get { return new Sprite[] { UnitSheetBuilder.sprites[ facing + mcvRange.Start ] }; }
		}

		public override IOrder Order( int2 xy )
		{
			if( ( fromCell == toCell || moveFraction == 0 ) && fromCell == xy )
				return new DeployMcvOrder( this );

			return base.Order( xy );
		}
	}
}
