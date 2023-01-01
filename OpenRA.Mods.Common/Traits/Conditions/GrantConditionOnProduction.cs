#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	public class GrantConditionOnProductionInfo : TraitInfo
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

		public override object Create(ActorInitializer init) { return new GrantConditionOnProduction(this); }
	}

	public class GrantConditionOnProduction : INotifyProduction, ITick, ISync, ISelectionBar
	{
		readonly GrantConditionOnProductionInfo info;

		int token = Actor.InvalidConditionToken;

		[Sync]
		int ticks;

		public GrantConditionOnProduction(GrantConditionOnProductionInfo info)
		{
			this.info = info;
			ticks = info.Duration;
		}

		void INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)
		{
			if (info.Actors.Count > 0 && !info.Actors.Select(a => a.ToLowerInvariant()).Contains(other.Info.Name))
				return;

			if (token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);

			ticks = info.Duration;
		}

		void ITick.Tick(Actor self)
		{
			if (info.Duration >= 0 && token != Actor.InvalidConditionToken && --ticks < 0)
				token = self.RevokeCondition(token);
		}

		float ISelectionBar.GetValue()
		{
			if (!info.ShowSelectionBar || info.Duration < 0 || token == Actor.InvalidConditionToken)
				return 0;

			return (float)ticks / info.Duration;
		}

		Color ISelectionBar.GetColor() { return info.SelectionBarColor; }
		bool ISelectionBar.DisplayWhenEmpty => false;
	}
}
