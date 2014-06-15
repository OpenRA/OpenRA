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
				return Util.SequenceActivities(new MoveAdjacentTo(self, target), this);

			// Move to the middle of the target, ignoring impassable tiles
			var move = self.Trait<IMove>();
			return Util.SequenceActivities(
				move.VisualMove(self, self.CenterPosition, target.CenterPosition),
				inner,
				move.VisualMove(self, target.CenterPosition, self.CenterPosition),
				NextActivity
			);
		}
	}
}
