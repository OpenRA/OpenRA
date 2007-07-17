using System;
using System.Collections.Generic;
using System.Text;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	class ConstructionYard : Actor
	{
		Animation animation = new Animation( "fact" );

		public ConstructionYard(float2 location, int palette)
		{
			this.renderLocation = location;
			this.palette = palette;
			animation.PlayToEnd( "make" );
		}

		public override Sprite[] CurrentImages
		{
			get { return animation.Images; }
		}

		public override void Tick( World world, double t )
		{
			animation.Tick( t );
		}
	}
}
