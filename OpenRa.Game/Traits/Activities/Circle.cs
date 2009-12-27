using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class Circle : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;
		readonly int2 Cell;

		public Circle(int2 cell) { Cell = cell; }

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			var unit = self.traits.Get<Unit>();
			return new Fly(Util.CenterOfCell(Cell))
			{
				NextActivity =
					new FlyTimed(50, 20)
					{
						NextActivity = this
					}
			};
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
