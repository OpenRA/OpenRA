#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Drag : Activity
	{
		readonly IPositionable positionable;
		readonly IMove movement;
		readonly IDisabledTrait disableable;
		readonly INotifyMoving[] notifyMoving;
		WPos start, end;
		int length;
		int ticks = 0;

		public Drag(Actor self, WPos start, WPos end, int length)
		{
			positionable = self.Trait<IPositionable>();
			movement = self.TraitOrDefault<IMove>();
			disableable = movement as IDisabledTrait;
			notifyMoving = self.TraitsImplementing<INotifyMoving>().ToArray();
			this.start = start;
			this.end = end;
			this.length = length;
			IsInterruptible = false;
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
				{
					movement.IsMoving = false;
					foreach (var n in notifyMoving)
						n.StoppedMoving(self);
				}

				return NextActivity;
			}

			if (movement != null)
			{
				movement.IsMoving = true;
				foreach (var n in notifyMoving)
					n.StartedMoving(self);
			}

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(end);
		}
	}
}
