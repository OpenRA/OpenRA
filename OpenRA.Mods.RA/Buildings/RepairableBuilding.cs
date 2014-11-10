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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	[Desc("Building can be repaired by the repair button.")]
	public class RepairableBuildingInfo : ITraitInfo, Requires<HealthInfo>
	{
		public readonly int RepairPercent = 20;
		public readonly int RepairInterval = 24;
		public readonly int RepairStep = 7;
		public readonly int[] RepairBonuses = { 100, 150, 175, 200, 220, 240, 260, 280, 300 };

		public readonly string IndicatorPalettePrefix = "player";

		public object Create(ActorInitializer init) { return new RepairableBuilding(init.self, this); }
	}

	public class RepairableBuilding : ITick, ISync
	{
		[Sync]
		public int RepairersHash { get { return Repairers.Aggregate(0, (code, player) => code ^ Sync.hash_player(player)); } }
		public List<Player> Repairers = new List<Player>();

		Health Health;
		RepairableBuildingInfo Info;
		public bool RepairActive = false;

		public RepairableBuilding(Actor self, RepairableBuildingInfo info)
		{
			Health = self.Trait<Health>();
			Info = info;
		}

		public void RepairBuilding(Actor self, Player player)
		{
			if (self.AppearsFriendlyTo(player.PlayerActor))
			{
				// If the player won't affect the repair, we won't add him
				if (!Repairers.Remove(player) && Repairers.Count < Info.RepairBonuses.Length) 
				{
					Repairers.Add(player);
					Sound.PlayNotification(self.World.Map.Rules, player, "Speech", "Repairing", player.Country.Race);

					self.World.AddFrameEndTask(w =>
					{
						if (!self.Flagged(ActorFlag.Dead))
							w.Add(new RepairIndicator(self, Info.IndicatorPalettePrefix));
					});
				}
			}
		}

		int remainingTicks;

		public void Tick(Actor self)
		{
			if (remainingTicks == 0)
			{
				Repairers = Repairers.Where(player => player.WinState == WinState.Undefined
						&& player.Stances[self.Owner] == Stance.Ally).ToList();

				// If after the previous operation there's no repairers left, stop
				if (!Repairers.Any()) return;
				var buildingValue = self.GetSellValue();

				// The cost is the same regardless of the amount of people repairing
				var hpToRepair = Math.Min(Info.RepairStep, Health.MaxHP - Health.HP);
				var cost = Math.Max(1, (hpToRepair * Info.RepairPercent * buildingValue) / (Health.MaxHP * 100));

				// TakeCash will return false if the player can't pay, and will stop him from contributing this Tick
				var activePlayers = Repairers.Count(player => player.PlayerActor.Trait<PlayerResources>().TakeCash(cost));

				RepairActive = activePlayers > 0;

				if (!RepairActive)
				{
					remainingTicks = 1;
					return;
				}

				// Bonus is applied after finding players who can pay
				
				// activePlayers won't cause IndexOutOfRange because we capped the max amount of players
				// to the length of the array
				self.InflictDamage(self, -(hpToRepair * Info.RepairBonuses[activePlayers - 1] / 100), null);

				if (Health.DamageState == DamageState.Undamaged)
				{
					Repairers.Clear();
					RepairActive = false;
					return;
				}

				remainingTicks = Info.RepairInterval;
			}
			else
				--remainingTicks;
		}
	}
}
