using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	class Building : Actor
	{
		protected Animation animation;
		protected int2 location;

		public Building( string name, int2 location, Player owner )
		{
			this.location = location;
			this.owner = owner;

			animation = new Animation( name );
			animation.PlayThen( "make", delegate { animation.Play( "idle" ); } );
		}

		public override void Tick( Game game, int t )
		{
			animation.Tick( t );
		}

		public override Sprite[] CurrentImages
		{
			get { return animation.Images; }
		}

		public override float2 RenderLocation
		{
			get { return 24.0f * (float2)location; }
		}
	}
}
