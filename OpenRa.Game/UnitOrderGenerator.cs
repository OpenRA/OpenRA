using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game
{
	class UnitOrderGenerator : IOrderGenerator
	{
		public readonly List<Actor> selection;

		public UnitOrderGenerator( IEnumerable<Actor> selected )
		{
			selection = selected.ToList();
		}

		public IEnumerable<Order> Order( Game game, int2 xy )
		{
			foreach( var unit in selection )
			{
				var ret = unit.Order( game, xy );
				if( ret != null )
					yield return ret;
			}
		}

		public void PrepareOverlay( Game game, int2 xy ) { }
	}
}
