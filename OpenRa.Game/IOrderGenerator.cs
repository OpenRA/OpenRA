using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	interface IOrderGenerator
	{
		IOrder Order( Game game, int2 xy );
		void PrepareOverlay( Game game, int2 xy );
	}
}
