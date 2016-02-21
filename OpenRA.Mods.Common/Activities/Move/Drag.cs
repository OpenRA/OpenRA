#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Drag : Activity
	{
		readonly IPositionable positionable;
		readonly IMove movement;
		readonly IDisabledTrait disableable;
		WPos start, end;
		int length;
		int ticks = 0;

		public Drag(Actor self, WPos start, WPos end, int length)
		{
			positionable = self.Trait<IPositionable>();
			movement = self.TraitOrDefault<IMove>();
			disableable = movement as IDisabledTrait;
			this.start = start;
			this.end = end;
			this.length = length;
		}

		public override Activity Tick(Actor self)
		{
			if (disableable != null && disableable.IsTraitDisabled)
				return this;

			var pos = length > 1
				? WPos.Lerp(start, end, ticks, length - 1)
				: end;

			positionable.SetVisualPosition(self, pos);
			if (++ticks >= length)
			{
				if (movement != null)
					movement.IsMoving = false;

				return NextActivity;
			}

			if (movement != null)
				movement.IsMoving = true;

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(end);
		}

		// Cannot be cancelled
		public override void Cancel(Actor self) { }
	}
}
