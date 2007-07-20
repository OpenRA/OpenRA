using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	class Truck : Unit
	{
		public Truck( int2 cell, int palette )
			: base( "truk", cell, palette, float2.Zero )
		{
		}
	}
}
