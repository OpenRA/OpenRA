#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Building can be repaired by the repair button.")]
	public class RepairableBuildingInfo : ConditionalTraitInfo, Requires<IHealthInfo>
	{
		[Desc("Cost to fully repair the actor as a percent of its value.")]
		public readonly int RepairPercent = 20;

		[Desc("Number of ticks between each repair step.")]
		public readonly int RepairInterval = 24;

		[Desc("The maximum amount of HP to repair each step.")]
		public readonly int RepairStep = 7;

		[Desc("Damage types used for the repair.")]
		public readonly BitSet<DamageType> RepairDamageTypes = default;

		[Desc("The percentage repair bonus applied with increasing numbers of repairers.")]
		public readonly int[] RepairBonuses = { 100, 150, 175, 200, 220, 240, 260, 280, 300 };

		// TODO: This should be replaced with a pause condition
		[Desc("Cancel the repair state when the trait is disabled.")]
		public readonly bool CancelWhenDisabled = false;

		[Desc("Experience gained by a player for repairing structures of allied players based on cost.")]
		public readonly int PlayerExperiencePercentage = 0;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while being repaired.")]
		public readonly string RepairCondition = null;

		[NotificationReference("Speech")]
		public readonly string RepairingNotification = null;

		public readonly string RepairingTextNotification = null;

		public override object Create(ActorInitializer init) { return new RepairableBuilding(init.Self, this); }
	}

	public class RepairableBuilding : ConditionalTrait<RepairableBuildingInfo>, ITick
	{
		readonly IHealth health;
		readonly Predicate<PlayerResources> isNotActiveAlly;
		readonly Stack<int> repairTokens = new Stack<int>();
		int remainingTicks;
		int builtUpCost = 0;
		int builtUpXP = 0;

		public readonly List<PlayerResources> Repairers = new List<PlayerResources>();
		public bool RepairActive { get; private set; }

		public RepairableBuilding(Actor self, RepairableBuildingInfo info)
			: base(info)
		{
			health = self.Trait<IHealth>();
			isNotActiveAlly = pr => pr.Owner.WinState != WinState.Undefined || self.Owner.RelationshipWith(pr.Owner) != PlayerRelationship.Ally;
		}

		[Sync]
		public int RepairersHash
		{
			get
			{
				var hash = 0;
				foreach (var player in Repairers)
					hash ^= Sync.HashPlayer(player.Owner);

				return hash;
			}
		}

		void UpdateCondition(Actor self)
		{
			if (string.IsNullOrEmpty(Info.RepairCondition))
				return;

			while (Repairers.Count > repairTokens.Count)
				repairTokens.Push(self.GrantCondition(Info.RepairCondition));

			while (Repairers.Count < repairTokens.Count && repairTokens.Count > 0)
				self.RevokeCondition(repairTokens.Pop());
		}

		public void RepairBuilding(Actor self, Player player)
		{
			if (IsTraitDisabled || !self.AppearsFriendlyTo(player.PlayerActor))
				return;

			var pr = player.PlayerActor.Trait<PlayerResources>();

			// Remove the player if they are already repairing
			if (Repairers.Remove(pr))
			{
				UpdateCondition(self);
				return;
			}

			// Don't add new players if the limit has already been reached
			if (Repairers.Count >= Info.RepairBonuses.Length - 1)
				return;

			Repairers.Add(pr);

			Game.Sound.PlayNotification(self.World.Map.Rules, player, "Speech", Info.RepairingNotification, player.Faction.InternalName);
			TextNotificationsManager.AddTransientLine(Info.RepairingTextNotification, self.Owner);

			UpdateCondition(self);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
			{
				if (RepairActive && Info.CancelWhenDisabled)
				{
					Repairers.Clear();
					UpdateCondition(self);
				}

				return;
			}

			if (remainingTicks == 0)
			{
				Repairers.RemoveAll(isNotActiveAlly);
				UpdateCondition(self);

				// If after the previous operation there's no repairers left, stop
				if (Repairers.Count == 0)
				{
					RepairActive = false;
					return;
				}

				var buildingValue = self.GetSellValue();

				// The cost is the same regardless of the amount of people repairing
				var hpToRepair = Math.Min(Info.RepairStep, health.MaxHP - health.HP);

				// Have all cost values multiplied by 100 as cost tends to be miniscule. 100x comes from Info.RepairPercent
				// Cast to long to avoid overflow
				var cost = (int)((long)hpToRepair * Info.RepairPercent * buildingValue / health.MaxHP);

				// We are using the highest possible repair cost to find the players that
				// can pay it as we cannot know what the actual cost will be
				var maxMultiplier = 0;
				for (var i = 0; i < Repairers.Count; i++)
				{
					var multiplier = Info.RepairBonuses[i] / (i + 1);
					if (multiplier > maxMultiplier)
						maxMultiplier = multiplier;
				}

				var activeRepairers = Repairers.Where(pr => pr.CanTakeCash(Math.Max(1, builtUpCost + cost * maxMultiplier / 100), true)).ToArray();
				RepairActive = activeRepairers.Any();

				if (!RepairActive)
				{
					remainingTicks = 1;
					return;
				}

				// Reduce or increase cost based on repair bonuses and amount of people repairing
				// We aren't using `*=` to avoid losing multipliers to integer truncation
				cost = cost * Info.RepairBonuses[activeRepairers.Length - 1] / activeRepairers.Length / 100;

				// Since we use 100x values we need to wait till we reach 100 to convert them back
				builtUpCost += cost;
				builtUpXP += cost * Info.PlayerExperiencePercentage / 100;

				if (builtUpCost >= 100)
				{
					var regularCost = builtUpCost / 100;
					builtUpCost -= regularCost * 100;

					foreach (var pr in activeRepairers)
						pr.TakeCash(regularCost, true);
				}

				if (builtUpXP >= 100)
				{
					var regularXP = builtUpXP / 100;
					builtUpXP -= regularXP * 100;
					foreach (var pr in activeRepairers)
						if (pr.Owner != self.Owner)
							pr.Owner.PlayerActor.TraitOrDefault<PlayerExperience>()?.GiveExperience(regularXP);
				}

				// activePlayers won't cause IndexOutOfRange because we capped the max amount of players
				// to the length of the array
				self.InflictDamage(self, new Damage(-hpToRepair * Info.RepairBonuses[activeRepairers.Length - 1] / 100, Info.RepairDamageTypes));

				if (health.DamageState == DamageState.Undamaged)
				{
					Repairers.Clear();
					RepairActive = false;
					UpdateCondition(self);
					return;
				}

				remainingTicks = Info.RepairInterval;
			}
			else
				--remainingTicks;
		}
	}
}
