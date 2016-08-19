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
		public bool TargetOverridesSelection(TargetModifiers modifiers) { return true; }

		public bool CanTarget(Actor self, Target target, ref IEnumerable<UIOrder> uiOrders, ref TargetModifiers modifiers)
		{
			return target.Type == TargetType.Actor && self == target.Actor;
		}

		public bool SetupTarget(Actor self, Target target, List<Actor> othersAtTarget, ref IEnumerable<UIOrder> uiOrders, ref TargetModifiers modifiers, ref string cursor)
		{
			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
			cursor = this.cursor();
			return true;
		}

		public void OrderIssued(Actor self) { }

		public bool IsQueued { get; protected set; }
	}
}
