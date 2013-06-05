#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Orders
{
	public class EnterOrderTargeter<T> : UnitOrderTargeter
	{
		readonly Func<Actor, bool> canTarget;
		readonly Func<Actor, bool> useEnterCursor;

		public EnterOrderTargeter(string order, int priority, bool targetEnemy, bool targetAlly,
			Func<Actor, bool> canTarget, Func<Actor, bool> useEnterCursor)
			: base (order, priority, "enter", targetEnemy, targetAlly)
		{
			this.canTarget = canTarget;
			this.useEnterCursor = useEnterCursor;
		}

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			if (!base.CanTargetActor(self, target, modifiers, ref cursor))
				return false;

			if (!target.HasTrait<T>())
				return false;

			if (!canTarget(target))
				return false;

			cursor = useEnterCursor(target) ? "enter" : "enter-blocked";
			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
			return true;
		}
	}
}
