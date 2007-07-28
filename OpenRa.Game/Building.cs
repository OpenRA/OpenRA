using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	class Building : PlayerOwned
	{
		public Building( string name, int2 location, Player owner, Game game )
			: base( game, name, location )
		{
			this.owner = owner;

			animation.PlayThen( "make", delegate { animation.PlayRepeating( "idle" ); } );
			owner.TechTree.Build( name, true );
		}

		public override void Tick( Game game, int t )
		{
			animation.Tick( t );
		}

	}
}
