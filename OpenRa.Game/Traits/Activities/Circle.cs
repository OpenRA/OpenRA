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
			return Util.SequenceActivities(
				new Fly(Util.CenterOfCell(Cell)),
				new FlyTimed(50, 20),
				this);
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
