#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class MoveOntoAndTurn : MoveOnto
	{
		readonly WAngle? desiredFacing;

		public MoveOntoAndTurn(Actor self, in Target target, in WVec offset, WAngle? desiredFacing, Color? targetLineColor = null)
			: base(self, target, offset, null, targetLineColor)
		{
			this.desiredFacing = desiredFacing;
		}

		public override bool Tick(Actor self)
		{
			if (base.Tick(self))
			{
				if (!IsCanceling && desiredFacing.HasValue && desiredFacing.Value != Mobile.Facing)
				{
					QueueChild(new Turn(self, desiredFacing.Value));
					return false;
				}

				return true;
			}

			return false;
		}
	}
}
