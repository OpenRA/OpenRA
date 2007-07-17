using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	class Building : Actor
	{
		protected Animation animation;

		public Building( string name, int2 location, int palette )
		{
			this.renderLocation = 24.0f * location.ToFloat2();
			this.palette = palette;

			animation = new Animation( name );
			animation.PlayThen( "make", delegate { animation.Play( "idle" ); } );
		}

		public override void Tick( World world, double t )
		{
			animation.Tick( t );
		}

		public override Sprite[] CurrentImages
		{
			get { return animation.Images; }
		}
	}
}
