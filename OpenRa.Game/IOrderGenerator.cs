using System.Collections.Generic;

namespace OpenRa.Game
{
	interface IOrderGenerator
	{
		IEnumerable<Order> Order( int2 xy, bool lmb );
		void Tick();
		void Render();
	}
}
