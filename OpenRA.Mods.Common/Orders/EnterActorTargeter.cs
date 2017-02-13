#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
	public class EnterActorTargeter<T> : UnitOrderTargeter where T : ITraitInfo
	{
		readonly Func<Actor, Actor, bool> canTarget;
		readonly Func<Actor, Actor, bool> useEnterCursor;

		public EnterActorTargeter(string order, int priority,
			Func<Actor, Actor, bool> canTarget, Func<Actor, Actor, bool> useEnterCursor)
			: base(order, priority, "enter", false, true)
		{
			this.canTarget = canTarget;
			this.useEnterCursor = useEnterCursor;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			if (!target.Info.HasTraitInfo<T>() || !canTarget(self, target))
				return false;

			cursor = useEnterCursor(self, target) ? "enter" : "enter-blocked";
			return true;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			// Allied actors are never frozen
			return false;
		}
	}
}
