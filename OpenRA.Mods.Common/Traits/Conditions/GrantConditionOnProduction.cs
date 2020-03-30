#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition when this actor produces a specific actor.")]
	public class GrantConditionOnProductionInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant")]
		public readonly string Condition = null;

		[ActorReference]
		[Desc("The actors to grant condition for. If empty condition will be granted for all actors.")]
		public readonly HashSet<string> Actors = new HashSet<string>();

		[Desc("How long condition is applies for. Use -1 for infinite.")]
		public readonly int Duration = -1;

		[Desc("Show a selection bar while condition is applied if it has a duration.")]
		public readonly bool ShowSelectionBar = true;
		public readonly Color SelectionBarColor = Color.Magenta;

		public object Create(ActorInitializer init) { return new GrantConditionOnProduction(init.Self, this); }
	}

	public class GrantConditionOnProduction : INotifyCreated, INotifyProduction, ITick, ISync, ISelectionBar
	{
		readonly GrantConditionOnProductionInfo info;
		ConditionManager conditionManager;

		int token = ConditionManager.InvalidConditionToken;

		[Sync]
		int ticks;

		public GrantConditionOnProduction(Actor self, GrantConditionOnProductionInfo info)
		{
			this.info = info;
			ticks = info.Duration;
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		void INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)
		{
			if (info.Actors.Any() && !info.Actors.Select(a => a.ToLowerInvariant()).Contains(other.Info.Name))
				return;

			if (conditionManager != null && token == ConditionManager.InvalidConditionToken)
				token = conditionManager.GrantCondition(self, info.Condition);

			ticks = info.Duration;
		}

		void ITick.Tick(Actor self)
		{
			if (info.Duration >= 0 && token != ConditionManager.InvalidConditionToken && --ticks < 0)
				token = conditionManager.RevokeCondition(self, token);
		}

		float ISelectionBar.GetValue()
		{
			if (info.Duration < 0 || token == ConditionManager.InvalidConditionToken)
				return 0;

			return (float)ticks / info.Duration;
		}

		Color ISelectionBar.GetColor() { return info.SelectionBarColor; }
		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
