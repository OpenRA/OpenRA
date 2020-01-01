#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		WPos start, end;
		int length;
		int ticks = 0;
		int desiredFacing;

		public Drag(Actor self, WPos start, WPos end, int length, int facing = -1)
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
			if (desiredFacing != -1)
				QueueChild(new Turn(self, desiredFacing));
		}

		public override bool Tick(Actor self)
		{
			if (disableable != null && disableable.IsTraitDisabled)
				return false;

			var pos = length > 1
				? WPos.Lerp(start, end, ticks, length - 1)
				: end;

			positionable.SetVisualPosition(self, pos);
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
