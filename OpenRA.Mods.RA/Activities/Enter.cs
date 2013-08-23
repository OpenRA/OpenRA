#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class Enter : Activity
	{
		readonly Target target;
		readonly Activity inner;

		public Enter(Actor target, Activity inner)
		{
			this.target = Target.FromActor(target);
			this.inner = inner;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			if (target.Type != TargetType.Actor)
				return NextActivity;

			if (!Util.AdjacentCells(target).Any(c => c == self.Location))
				return Util.SequenceActivities(new MoveAdjacentTo(target), this);

			// Move to the middle of the target, ignoring impassable tiles
			var mobile = self.Trait<Mobile>();
			var to = target.CenterPosition;
			var from = self.CenterPosition;
			var speed = mobile.MovementSpeedForCell(self, self.Location);
			var length = speed > 0 ? (to - from).Length / speed : 0;

			return Util.SequenceActivities(
				new Turn(Util.GetFacing(to - from, mobile.Facing)),
				new Drag(from, to, length),
				inner,
				new Turn(Util.GetFacing(from - to, mobile.Facing)),
				new Drag(to, from, length),
				NextActivity
			);
		}
	}
}
