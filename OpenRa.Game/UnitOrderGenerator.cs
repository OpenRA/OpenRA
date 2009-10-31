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

		public IEnumerable<Order> Order( int2 xy, bool lmb )
		{
			foreach( var unit in selection )
			{
				var ret = unit.Order( xy, lmb );
				if( ret != null )
					yield return ret;
			}
		}

		public void Tick()
		{
			selection.RemoveAll(a => a.IsDead);
		}
	}
}
