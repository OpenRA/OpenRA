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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants an upgrade to the collector.")]
	public class GrantUpgradeCrateActionInfo : CrateActionInfo
	{
		[UpgradeGrantedReference, FieldLoader.Require]
		[Desc("The upgrades to apply.")]
		public readonly string[] Upgrades = { };

		[Desc("Duration of the upgrade (in ticks). Set to 0 for a permanent upgrade.")]
		public readonly int Duration = 0;

		[Desc("The range to search for extra collectors in.", "Extra collectors will also be granted the crate action.")]
		public readonly WDist Range = new WDist(3);

		[Desc("The maximum number of extra collectors to grant the crate action to.", "-1 = no limit")]
		public readonly int MaxExtraCollectors = 4;

		public override object Create(ActorInitializer init) { return new GrantUpgradeCrateAction(init.Self, this); }
	}

	public class GrantUpgradeCrateAction : CrateAction
	{
		readonly Actor self;
		readonly GrantUpgradeCrateActionInfo info;

		public GrantUpgradeCrateAction(Actor self, GrantUpgradeCrateActionInfo info)
			: base(self, info)
		{
			this.self = self;
			this.info = info;
		}

		bool AcceptsUpgrade(Actor a)
		{
			var um = a.TraitOrDefault<UpgradeManager>();
			return um != null && (info.Duration > 0 ?
				info.Upgrades.Any(u => um.AcknowledgesUpgrade(a, u)) : info.Upgrades.Any(u => um.AcceptsUpgrade(a, u)));
		}

		public override int GetSelectionShares(Actor collector)
		{
			return AcceptsUpgrade(collector) ? info.SelectionShares : 0;
		}

		public override void Activate(Actor collector)
		{
			var actorsInRange = self.World.FindActorsInCircle(self.CenterPosition, info.Range)
				.Where(a => a != self && a != collector && a.Owner == collector.Owner && AcceptsUpgrade(a));

			if (info.MaxExtraCollectors > -1)
				actorsInRange = actorsInRange.Take(info.MaxExtraCollectors);

			collector.World.AddFrameEndTask(w =>
			{
				foreach (var a in actorsInRange.Append(collector))
				{
					if (!a.IsInWorld || a.IsDead)
						continue;

					var um = a.TraitOrDefault<UpgradeManager>();
					foreach (var u in info.Upgrades)
					{
						if (info.Duration > 0)
						{
							if (um.AcknowledgesUpgrade(a, u))
								um.GrantTimedUpgrade(a, u, info.Duration);
						}
						else
						{
							if (um.AcceptsUpgrade(a, u))
								um.GrantUpgrade(a, u, this);
						}
					}
				}
			});

			base.Activate(collector);
		}
	}
}