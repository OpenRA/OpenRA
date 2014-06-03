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
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA.Buildings
{
	[Desc("Building can be repaired by the repair button.")]
	public class RepairableBuildingInfo : ITraitInfo, Requires<HealthInfo>
	{
		public readonly int RepairPercent = 20;
		public readonly int RepairInterval = 24;
		public readonly int RepairStep = 7;
		public readonly string IndicatorPalettePrefix = "player";

		public object Create(ActorInitializer init) { return new RepairableBuilding(init.self, this); }
	}

	public class RepairableBuilding : ITick, ISync
	{
		[Sync] public Player Repairer = null;

		Health Health;
		RepairableBuildingInfo Info;
		public bool RepairActive = true;

		public RepairableBuilding(Actor self, RepairableBuildingInfo info)
		{
			Health = self.Trait<Health>();
			Info = info;
		}

		public void RepairBuilding(Actor self, Player p)
		{
			if (self.HasTrait<RepairableBuilding>())
			{
				if (self.AppearsFriendlyTo(p.PlayerActor))
				{
					if (Repairer == p)
						Repairer = null;

					else
					{
						Repairer = p;
						Sound.PlayNotification(self.World.Map.Rules, Repairer, "Speech", "Repairing", self.Owner.Country.Race);

						self.World.AddFrameEndTask(w =>
						{
							if (!self.IsDead())
								w.Add(new RepairIndicator(self, Info.IndicatorPalettePrefix, p));
						});
					}
				}
			}
		}

		int remainingTicks;

		public void Tick(Actor self)
		{
			if (Repairer == null) return;

			if (remainingTicks == 0)
			{
				if (Repairer.WinState != WinState.Undefined || Repairer.Stances[self.Owner] != Stance.Ally)
				{
					Repairer = null;
					return;
				}

				var buildingValue = self.GetSellValue();

				var hpToRepair = Math.Min(Info.RepairStep, Health.MaxHP - Health.HP);
				var cost = Math.Max(1, (hpToRepair * Info.RepairPercent * buildingValue) / (Health.MaxHP * 100));
				RepairActive = Repairer.PlayerActor.Trait<PlayerResources>().TakeCash(cost);
				if (!RepairActive)
				{
					remainingTicks = 1;
					return;
				}

				self.InflictDamage(self, -hpToRepair, null);

				if (Health.DamageState == DamageState.Undamaged)
				{
					Repairer = null;
					return;
				}

				remainingTicks = Info.RepairInterval;
			}
			else
				--remainingTicks;
		}
	}
}
