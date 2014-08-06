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

namespace OpenRA.Mods.RA.Crates
{
	[Desc("Grants an upgrade to the collector.")]
	public class UnitUpgradeCrateActionInfo : CrateActionInfo
	{
		[Desc("The upgrade to grant.")]
		public readonly UnitUpgrade? Upgrade = null;

		[Desc("The number of levels of the upgrade to grant.")]
		public readonly int Levels = 1;

		[Desc("The range to search for extra collectors in.","Extra collectors will also be granted the crate action.")]
		public readonly WRange Range = new WRange(3);

		[Desc("The maximum number of extra collectors to grant the crate action to.","-1 = no limit")]
		public readonly int MaxExtraCollectors = 4;

		public override object Create(ActorInitializer init) { return new UnitUpgradeCrateAction(init.self, this); }
	}

	public class UnitUpgradeCrateAction : CrateAction
	{
		UnitUpgradeCrateActionInfo Info;

		public UnitUpgradeCrateAction(Actor self, UnitUpgradeCrateActionInfo info)
			: base(self, info) 
		{
			Info = info; 
		}

		public override int GetSelectionShares(Actor collector)
		{
			var up = collector.TraitOrDefault<GainsUnitUpgrades>();
			return up != null && up.CanGainUnitUpgrade(Info.Upgrade) ? info.SelectionShares : 0;
		}

		public override void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w =>
			{
				var gainsStatBonuses = collector.TraitOrDefault<GainsUnitUpgrades>();
				if (gainsStatBonuses != null)
					gainsStatBonuses.GiveUnitUpgrade(Info.Upgrade, Info.Levels);
			});

			var inRange = self.World.FindActorsInCircle(self.CenterPosition, Info.Range);
			inRange = inRange.Where(a =>
				(a.Owner == collector.Owner) &&
				(a != collector) &&
				(a.TraitOrDefault<GainsUnitUpgrades>() != null) &&
				(a.TraitOrDefault<GainsUnitUpgrades>().CanGainUnitUpgrade(Info.Upgrade)));
			if (inRange.Any())
			{
				if (Info.MaxExtraCollectors > -1)
					inRange = inRange.Take(Info.MaxExtraCollectors);

				if (inRange.Any())
					foreach (var actor in inRange)
					{
						actor.World.AddFrameEndTask(w =>
						{
							var gainsStatBonuses = actor.TraitOrDefault<GainsUnitUpgrades>();
							if (gainsStatBonuses != null)
								gainsStatBonuses.GiveUnitUpgrade(Info.Upgrade, Info.Levels);
						});
					}
			}

			base.Activate(collector);
		}
	}
}