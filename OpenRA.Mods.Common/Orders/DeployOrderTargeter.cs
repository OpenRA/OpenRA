#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public class DeployOrderTargeter : IOrderTargeter
	{
		readonly Func<bool> useDeployCursor;

		public DeployOrderTargeter( string order, int priority )
			: this( order, priority, () => true )
		{
		}

		public DeployOrderTargeter( string order, int priority, Func<bool> useDeployCursor )
		{
			this.OrderID = order;
			this.OrderPriority = priority;
			this.useDeployCursor = useDeployCursor;
		}

		public string OrderID { get; private set; }
		public int OrderPriority { get; private set; }

		public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, TargetModifiers modifiers, ref string cursor)
		{
			if (target.Type != TargetType.Actor)
				return false;

			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);
			cursor = useDeployCursor() ? "deploy" : "deploy-blocked";

			return self == target.Actor;
		}

		public bool IsQueued { get; protected set; }
	}
}
