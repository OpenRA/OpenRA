using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	interface IOrderGenerator
	{
		IEnumerable<Order> Order( int2 xy, bool lmb );
		void Tick();
		void Render();
	}
}
