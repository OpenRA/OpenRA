#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public class EnterAlliedActorTargeter<T> : UnitOrderTargeter where T : ITraitInfo
	{
		private const string EnterTargetCursor = "enter";
		private const string EnterTargetBlockedCursor = "enter-blocked";

		readonly Func<Actor, bool> canTarget;
		readonly Func<Actor, bool> useEnterCursor;

		public EnterAlliedActorTargeter(string order, int priority,
			Func<Actor, bool> canTarget, Func<Actor, bool> useEnterCursor)
			: base(order, priority, EnterTargetCursor, false, true)
		{
			this.canTarget = canTarget;
			this.useEnterCursor = useEnterCursor;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			if (!target.Info.HasTraitInfo<T>() || !canTarget(target))
				return false;

			cursor = useEnterCursor(target) ? EnterTargetCursor : EnterTargetBlockedCursor;
			return true;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			// Allied actors are never frozen
			return false;
		}
	}
}
