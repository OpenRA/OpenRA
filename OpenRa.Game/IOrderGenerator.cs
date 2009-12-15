using System.Collections.Generic;

namespace OpenRa.Game
{
	interface IOrderGenerator
	{
		IEnumerable<Order> Order( int2 xy, MouseInput mi );
		void Tick();
		void Render();
	}
}
