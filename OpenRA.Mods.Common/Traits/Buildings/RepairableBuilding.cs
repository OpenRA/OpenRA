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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Building can be repaired by the repair button.")]
	public class RepairableBuildingInfo : UpgradableTraitInfo, Requires<HealthInfo>
	{
		public readonly int RepairPercent = 20;
		public readonly int RepairInterval = 24;
		public readonly int RepairStep = 7;
		public readonly int[] RepairBonuses = { 100, 150, 175, 200, 220, 240, 260, 280, 300 };
		public readonly bool CancelWhenDisabled = false;

		public readonly string IndicatorImage = "allyrepair";
		[SequenceReference("IndicatorImage")] public readonly string IndicatorSequence = "repair";

		[Desc("Overrides the IndicatorPalettePrefix.")]
		[PaletteReference] public readonly string IndicatorPalette = "";

		[Desc("Suffixed by the internal repairing player name.")]
		public readonly string IndicatorPalettePrefix = "player";

		public override object Create(ActorInitializer init) { return new RepairableBuilding(init.Self, this); }
	}

	public class RepairableBuilding : UpgradableTrait<RepairableBuildingInfo>, ITick
	{
		[Sync]
		public int RepairersHash
		{
			get
			{
				var hash = 0;
				foreach (var player in Repairers)
					hash ^= Sync.HashPlayer(player);
				return hash;
			}
		}

		public readonly List<Player> Repairers = new List<Player>();

		readonly Health health;
		public bool RepairActive = false;

		readonly Predicate<Player> isNotActiveAlly;

		public RepairableBuilding(Actor self, RepairableBuildingInfo info)
			: base(info)
		{
			health = self.Trait<Health>();
			isNotActiveAlly = player => player.WinState != WinState.Undefined || player.Stances[self.Owner] != Stance.Ally;
		}

		public void RepairBuilding(Actor self, Player player)
		{
			if (!IsTraitDisabled && self.AppearsFriendlyTo(player.PlayerActor))
			{
				// If the player won't affect the repair, we won't add him
				if (!Repairers.Remove(player) && Repairers.Count < Info.RepairBonuses.Length)
				{
					Repairers.Add(player);
					Game.Sound.PlayNotification(self.World.Map.Rules, player, "Speech", "Repairing", player.Faction.InternalName);

					self.World.AddFrameEndTask(w =>
					{
						if (!self.IsDead)
							w.Add(new RepairIndicator(self));
					});
				}
			}
		}

		int remainingTicks;

		public void Tick(Actor self)
		{
			if (IsTraitDisabled)
			{
				if (RepairActive && Info.CancelWhenDisabled)
				{
					Repairers.Clear();
					RepairActive = false;
				}

				return;
			}

			if (remainingTicks == 0)
			{
				Repairers.RemoveAll(isNotActiveAlly);

				// If after the previous operation there's no repairers left, stop
				if (Repairers.Count == 0)
					return;

				var buildingValue = self.GetSellValue();

				// The cost is the same regardless of the amount of people repairing
				var hpToRepair = Math.Min(Info.RepairStep, health.MaxHP - health.HP);
				var cost = Math.Max(1, (hpToRepair * Info.RepairPercent * buildingValue) / (health.MaxHP * 100));

				// TakeCash will return false if the player can't pay, and will stop him from contributing this Tick
				var activePlayers = Repairers.Count(player => player.PlayerActor.Trait<PlayerResources>().TakeCash(cost, true));

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

				if (health.DamageState == DamageState.Undamaged)
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
