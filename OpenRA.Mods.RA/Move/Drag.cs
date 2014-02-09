#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Move
{
	public class Drag : Activity
	{
		WPos start, end;
		int length;
		int ticks = 0;

		public Drag(WPos start, WPos end, int length)
		{
			this.start = start;
			this.end = end;
			this.length = length;
		}

		public override Activity Tick(Actor self)
		{
			var positionable = self.Trait<IPositionable>();
			var movement = self.TraitOrDefault<IMove>();

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
