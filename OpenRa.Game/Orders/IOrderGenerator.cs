using System.Collections.Generic;

namespace OpenRa
{
	public interface IOrderGenerator
	{
		IEnumerable<Order> Order( int2 xy, MouseInput mi );
		void Tick();
		void Render();
		Cursor GetCursor(int2 xy, MouseInput mi);
	}
}
