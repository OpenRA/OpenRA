#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Drag : Activity
	{
		readonly IPositionable positionable;
		readonly IDisabledTrait disableable;
		readonly WPos start;
		readonly WPos end;
		readonly int length;
		int ticks = 0;
		readonly WAngle? desiredFacing;

		public Drag(Actor self, WPos start, WPos end, int length, WAngle? facing = null)
		{
			positionable = self.Trait<IPositionable>();
			disableable = self.TraitOrDefault<IMove>() as IDisabledTrait;
			this.start = start;
			this.end = end;
			this.length = length;
			desiredFacing = facing;
			IsInterruptible = false;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (desiredFacing.HasValue)
				QueueChild(new Turn(self, desiredFacing.Value));
		}

		public override bool Tick(Actor self)
		{
			if (disableable != null && disableable.IsTraitDisabled)
				return false;

			var pos = length > 1
				? WPos.Lerp(start, end, ticks, length - 1)
				: end;

			positionable.SetCenterPosition(self, pos);
			if (++ticks >= length)
				return true;

			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(end);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(Target.FromPos(end), Color.Green);
		}
	}
}
