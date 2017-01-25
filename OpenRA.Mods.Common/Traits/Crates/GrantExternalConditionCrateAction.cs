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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition on the collector.")]
	public class GrantExternalConditionCrateActionInfo : CrateActionInfo
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent condition.")]
		public readonly int Duration = 0;

		[Desc("The range to search for extra collectors in.", "Extra collectors will also be granted the crate action.")]
		public readonly WDist Range = new WDist(3);

		[Desc("The maximum number of extra collectors to grant the crate action to.", "-1 = no limit")]
		public readonly int MaxExtraCollectors = 4;

		public override object Create(ActorInitializer init) { return new GrantExternalConditionCrateAction(init.Self, this); }
	}

	public class GrantExternalConditionCrateAction : CrateAction
	{
		readonly Actor self;
		readonly GrantExternalConditionCrateActionInfo info;

		public GrantExternalConditionCrateAction(Actor self, GrantExternalConditionCrateActionInfo info)
			: base(self, info)
		{
			this.self = self;
			this.info = info;
		}

		bool AcceptsCondition(Actor a)
		{
			var cm = a.TraitOrDefault<ConditionManager>();
			return cm != null && cm.AcceptsExternalCondition(a, info.Condition, info.Duration > 0);
		}

		public override int GetSelectionShares(Actor collector)
		{
			return AcceptsCondition(collector) ? info.SelectionShares : 0;
		}

		public override void Activate(Actor collector)
		{
			var actorsInRange = self.World.FindActorsInCircle(self.CenterPosition, info.Range)
				.Where(a => a != self && a != collector && a.Owner == collector.Owner && AcceptsCondition(a));

			if (info.MaxExtraCollectors > -1)
				actorsInRange = actorsInRange.Take(info.MaxExtraCollectors);

			collector.World.AddFrameEndTask(w =>
			{
				foreach (var a in actorsInRange.Append(collector))
				{
					if (!a.IsInWorld || a.IsDead)
						continue;

					var cm = a.TraitOrDefault<ConditionManager>();

					// Condition token is ignored because we never revoke this condition.
					if (cm != null)
						cm.GrantCondition(a, info.Condition, true, info.Duration);
				}
			});

			base.Activate(collector);
		}
	}
}