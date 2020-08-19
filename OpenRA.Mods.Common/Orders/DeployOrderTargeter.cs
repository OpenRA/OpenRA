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

using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public class DeployOrderTargeter : IOrderTargeter
	{
		readonly Func<string> cursor;

		public DeployOrderTargeter(string order, int priority)
			: this(order, priority, () => "deploy")
		{
		}

		public DeployOrderTargeter(string order, int priority, Func<string> cursor)
		{
			OrderID = order;
			OrderPriority = priority;
			this.cursor = cursor;
		}

		public string OrderID { get; private set; }
		public int OrderPriority { get; private set; }
		public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers) { return true; }

		public bool CanTarget(Actor self, in Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
		{
			if (target.Type != TargetType.Actor)
				return false;

			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
			cursor = this.cursor();

			return self == target.Actor;
		}

		public bool IsQueued { get; protected set; }
	}
}
