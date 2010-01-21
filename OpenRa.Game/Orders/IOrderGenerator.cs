using System.Collections.Generic;

namespace OpenRa
{
	public interface IOrderGenerator
	{
		IEnumerable<Order> Order( World world, int2 xy, MouseInput mi );
		void Tick( World world );
		void Render( World world );
		Cursor GetCursor( World world, int2 xy, MouseInput mi );
	}
}
