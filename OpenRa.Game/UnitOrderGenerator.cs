using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OpenRa.Game
{
	class UnitOrderGenerator : IOrderGenerator
	{
		public readonly List<Actor> selection;

		public UnitOrderGenerator( IEnumerable<Actor> selected )
		{
			selection = selected.ToList();
		}

		public IEnumerable<Order> Order( int2 xy, MouseInput mi )
		{
			foreach( var unit in selection )
			{
				var ret = unit.Order( xy, mi );
				if( ret != null )
					yield return ret;
			}
		}

		public void Tick()
		{
			selection.RemoveAll(a => a.IsDead);
		}

		public void Render()
		{
			foreach( var a in selection )
				Game.worldRenderer.DrawSelectionBox( a, Color.White, true );
		}
	}
}
