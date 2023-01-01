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

using System.Linq;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition on the collector.")]
	public class GrantExternalConditionCrateActionInfo : CrateActionInfo
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		[Desc("How many times to grant the condition.")]
		public readonly int Levels = 1;

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
			return a.TraitsImplementing<ExternalCondition>()
				.Any(t => t.Info.Condition == info.Condition && t.CanGrantCondition(self));
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

					var externals = a.TraitsImplementing<ExternalCondition>()
						.Where(t => t.Info.Condition == info.Condition);

					ExternalCondition external = null;
					for (var n = 0; n < info.Levels; n++)
					{
						if (external == null || !external.CanGrantCondition(self))
						{
							external = externals.FirstOrDefault(t => t.CanGrantCondition(self));
							if (external == null)
								break;
						}

						external.GrantCondition(a, self, info.Duration);
					}
				}
			});

			base.Activate(collector);
		}
	}
}
