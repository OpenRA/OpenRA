using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	class MoveOrder
	{
		public readonly Mcv Unit;
		public readonly float2 Destination;

		public MoveOrder( Mcv unit, int x, int y )
			: this( unit, new float2( x * 24, y * 24 ) )
		{
		}

		public MoveOrder(Mcv unit, float2 destination)
		{
			this.Unit = unit;
			this.Destination = destination;
		}

		public void Apply()
		{
			Unit.Accept( this );
		}
	}
}
