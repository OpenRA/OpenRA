using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	interface IOrderGenerator
	{
		Order Order( Game game, int2 xy );
		void PrepareOverlay( Game game, int2 xy );
	}
}
