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
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Crates
{
	[Desc("Grants an upgrade to the collector.")]
	public class UnitUpgradeCrateActionInfo : CrateActionInfo
	{
		[Desc("The upgrade to grant.")]
		public readonly string[] Upgrades = {};

		[Desc("The range to search for extra collectors in.", "Extra collectors will also be granted the crate action.")]
		public readonly WRange Range = new WRange(3);

		[Desc("The maximum number of extra collectors to grant the crate action to.", "-1 = no limit")]
		public readonly int MaxExtraCollectors = 4;

		public override object Create(ActorInitializer init) { return new UnitUpgradeCrateAction(init.self, this); }
	}

	public class UnitUpgradeCrateAction : CrateAction
	{
		readonly UnitUpgradeCrateActionInfo Info;

		public UnitUpgradeCrateAction(Actor self, UnitUpgradeCrateActionInfo info)
			: base(self, info) 
		{
			Info = info; 
		}

		bool AcceptsUpgrade(Actor a)
		{
			return a.TraitsImplementing<IUpgradable>()
				.Any(up => Info.Upgrades.Any(u => up.AcceptsUpgrade(u)));
		}

		void GrantActorUpgrades(Actor a)
		{
			foreach (var up in a.TraitsImplementing<IUpgradable>())
				foreach (var u in Info.Upgrades)
					if (up.AcceptsUpgrade(u))
						up.UpgradeAvailable(a, u, true);
		}

		public override int GetSelectionShares(Actor collector)
		{
			return AcceptsUpgrade(collector) ? info.SelectionShares : 0;
		}

		public override void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w => GrantActorUpgrades(collector));

			var actorsInRange = self.World.FindActorsInCircle(self.CenterPosition, Info.Range)
				.Where(a => a != self && a.Owner == collector.Owner && AcceptsUpgrade(a));

			if (actorsInRange.Any())
			{
				if (Info.MaxExtraCollectors > -1)
					actorsInRange = actorsInRange.Take(Info.MaxExtraCollectors);

				collector.World.AddFrameEndTask(w =>
				{
					foreach (var a in actorsInRange)
					{
						if (!a.IsDead() && a.IsInWorld)
							GrantActorUpgrades(a);
					}
				});
			}

			base.Activate(collector);
		}
	}
}