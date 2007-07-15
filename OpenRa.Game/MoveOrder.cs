using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	class MoveOrder
	{
		public readonly float2 Destination;

		public MoveOrder(float2 destination)
		{
			this.Destination = destination - new float2(24,24);		//HACK account for MCV size
		}
	}
}
