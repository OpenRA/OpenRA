using System;
using System.Collections.Generic;
using System.Text;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	class ConstructionYard : Building
	{
		public ConstructionYard( int2 location, Player owner, Game game )
			: base( "fact", location, owner, game )
		{
			animation.PlayThen( "make", delegate { animation.PlayRepeating( "build" ); } );
		}
	}
}
