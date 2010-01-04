using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits.Activities
{
	class UnloadCargo : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;

			// if we're a thing that can turn, turn to the
			// right facing for the unload animation
			var unit = self.traits.GetOrDefault<Unit>();
			if (unit != null && unit.Facing != self.Info.UnloadFacing)
				return Util.SequenceActivities(new Turn(self.Info.UnloadFacing), this);

			// todo: play the `open` anim (or the `close` anim backwards)
			// todo: unload all the cargo
			// todo: play the `close` anim (or the `open` anim backwards)

			// as for open/close... the westwood guys suck at being consistent.

			return this;
		}

		public void Cancel(Actor self) { NextActivity = null; isCanceled = true; }
	}
}
