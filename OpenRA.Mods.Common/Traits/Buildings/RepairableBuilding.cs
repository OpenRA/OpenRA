#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	public class RepairableBuildingInfo : ConditionalTraitInfo, Requires<HealthInfo>
	{
		[Desc("Cost to fully repair the actor as a percent of its value.")]
		public readonly int RepairPercent = 20;

		[Desc("Number of ticks between each repair step.")]
		public readonly int RepairInterval = 24;

		[Desc("The maximum amount of HP to repair each step.")]
		public readonly int RepairStep = 7;

		[Desc("The percentage repair bonus applied with increasing numbers of repairers.")]
		public readonly int[] RepairBonuses = { 100, 150, 175, 200, 220, 240, 260, 280, 300 };

		// TODO: This should be replaced with a pause condition
		[Desc("Cancel the repair state when the trait is disabled.")]
		public readonly bool CancelWhenDisabled = false;

		[Desc("Experience gained by a player for repairing structures of allied players.")]
		public readonly int PlayerExperience = 0;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while being repaired.")]
		public readonly string RepairCondition = null;

		public override object Create(ActorInitializer init) { return new RepairableBuilding(init.Self, this); }
	}

	public class RepairableBuilding : ConditionalTrait<RepairableBuildingInfo>, ITick
	{
		readonly Health health;
		readonly Predicate<Player> isNotActiveAlly;
		readonly Stack<int> repairTokens = new Stack<int>();
		ConditionManager conditionManager;
		int remainingTicks;

		public readonly List<Player> Repairers = new List<Player>();
		public bool RepairActive { get; private set; }

		public RepairableBuilding(Actor self, RepairableBuildingInfo info)
			: base(info)
		{
			health = self.Trait<Health>();
			isNotActiveAlly = player => player.WinState != WinState.Undefined || player.Stances[self.Owner] != Stance.Ally;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

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

		void UpdateCondition(Actor self)
		{
			if (conditionManager == null || string.IsNullOrEmpty(Info.RepairCondition))
				return;

			var currentRepairers = Repairers.Count;
			while (Repairers.Count > repairTokens.Count)
				repairTokens.Push(conditionManager.GrantCondition(self, Info.RepairCondition));

			while (Repairers.Count < repairTokens.Count && repairTokens.Count > 0)
				conditionManager.RevokeCondition(self, repairTokens.Pop());
		}

		public void RepairBuilding(Actor self, Player player)
		{
			if (IsTraitDisabled || !self.AppearsFriendlyTo(player.PlayerActor))
				return;

			// Remove the player if they are already repairing
			if (Repairers.Remove(player))
			{
				UpdateCondition(self);
				return;
			}

			// Don't add new players if the limit has already been reached
			if (Repairers.Count >= Info.RepairBonuses.Length - 1)
				return;

			Repairers.Add(player);
			Game.Sound.PlayNotification(self.World.Map.Rules, player, "Speech", "Repairing", player.Faction.InternalName);
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

				// Cast to long to avoid overflow when multiplying by the health
				var cost = Math.Max(1, (int)(((long)hpToRepair * Info.RepairPercent * buildingValue) / (health.MaxHP * 100L)));

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
				self.InflictDamage(self, new Damage(-(hpToRepair * Info.RepairBonuses[activePlayers - 1] / 100)));

				if (health.DamageState == DamageState.Undamaged)
				{
					Repairers.Do(r =>
					{
						if (r == self.Owner)
							return;

						var exp = r.PlayerActor.TraitOrDefault<PlayerExperience>();
						if (exp != null)
							exp.GiveExperience(Info.PlayerExperience);
					});

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
