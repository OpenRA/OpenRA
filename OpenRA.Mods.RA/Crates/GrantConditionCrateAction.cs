#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Crates
{
	[Desc("Grants a condition to the collector.")]
	public class GrantConditionCrateActionInfo : CrateActionInfo
	{
		[Desc("The condition types to apply.")]
		public readonly string[] Conditions = { };

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent condition.")]
		public readonly int Duration = 0;

		[Desc("The range to search for extra collectors in.", "Extra collectors will also be granted the crate action.")]
		public readonly WRange Range = new WRange(3);

		[Desc("The maximum number of extra collectors to grant the crate action to.", "-1 = no limit")]
		public readonly int MaxExtraCollectors = 4;

		public override object Create(ActorInitializer init) { return new GrantConditionCrateAction(init.self, this); }
	}

	public class GrantConditionCrateAction : CrateAction
	{
		readonly Actor self;
		readonly GrantConditionCrateActionInfo info;

		public GrantConditionCrateAction(Actor self, GrantConditionCrateActionInfo info)
			: base(self, info) 
		{
			this.self = self;
			this.info = info;
		}

		bool AcceptsConditionType(Actor a)
		{
			var um = a.TraitOrDefault<ConditionManager>();
			return um != null && info.Conditions.Any(u => um.AcceptsConditionType(a, u));
		}

		public override int GetSelectionShares(Actor collector)
		{
			return AcceptsConditionType(collector) ? info.SelectionShares : 0;
		}

		public override void Activate(Actor collector)
		{
			var actorsInRange = self.World.FindActorsInCircle(self.CenterPosition, info.Range)
				.Where(a => a != self && a != collector && a.Owner == collector.Owner && AcceptsConditionType(a));

			if (info.MaxExtraCollectors > -1)
				actorsInRange = actorsInRange.Take(info.MaxExtraCollectors);

			collector.World.AddFrameEndTask(w =>
			{
				foreach (var a in actorsInRange.Append(collector))
				{
					if (!a.IsInWorld || a.IsDead)
						continue;

					var um = a.TraitOrDefault<ConditionManager>();
					foreach (var u in info.Conditions)
					{
						if (!um.AcceptsConditionType(a, u))
							continue;

						if (info.Duration > 0)
							um.GrantTimedCondition(a, u, info.Duration);
						else
							um.GrantCondition(a, u, this);
					}
				}
			});

			base.Activate(collector);
		}
	}
}