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

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public class EnterAlliedActorTargeter<T> : UnitOrderTargeter where T : ITraitInfo
	{
		readonly Func<Actor, bool> canTarget;
		readonly Func<Actor, bool> useEnterCursor;

		public EnterAlliedActorTargeter(string order, int priority,
			Func<Actor, bool> canTarget, Func<Actor, bool> useEnterCursor)
			: base(order, priority, "enter", false, true)
		{
			this.canTarget = canTarget;
			this.useEnterCursor = useEnterCursor;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			if (!self.Owner.IsAlliedWith(target.Owner) || !target.Info.HasTraitInfo<T>() || !canTarget(target))
				return false;

			cursor = useEnterCursor(target) ? "enter" : "enter-blocked";
			return true;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			// Allied actors are never frozen
			return false;
		}
	}
}
