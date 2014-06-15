#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.RA.Crates
{
	public class UnitUpgradeCrateActionInfo : CrateActionInfo
	{
		public readonly UnitUpgrade? Upgrade = null;
		public readonly int Levels = 1;

		public override object Create(ActorInitializer init) { return new UnitUpgradeCrateAction(init.self, this); }
	}

	public class UnitUpgradeCrateAction : CrateAction
	{
		UnitUpgradeCrateActionInfo crateInfo;

		public UnitUpgradeCrateAction(Actor self, UnitUpgradeCrateActionInfo info)
			: base(self, info) 
		{
			crateInfo = info; 
		}

		public override int GetSelectionShares(Actor collector)
		{
			var up = collector.TraitOrDefault<GainsUnitUpgrades>();
			return up != null && up.CanGainUnitUpgrade(crateInfo.Upgrade) ? info.SelectionShares : 0;
		}

		public override void Activate(Actor collector)
		{
			collector.World.AddFrameEndTask(w =>
			{
				var gainsStatBonuses = collector.TraitOrDefault<GainsUnitUpgrades>();
				if (gainsStatBonuses != null)
					gainsStatBonuses.GiveUnitUpgrade(crateInfo.Upgrade, crateInfo.Levels);
			});

			base.Activate(collector);
		}
	}
}