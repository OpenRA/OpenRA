#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI repairing base buildings.")]
	public class BuildingRepairBotModuleInfo : ConditionalTraitInfo
	{
		[Desc($"A delay (in ticks) of repair all actors with {nameof(RepairableBuilding)} periodically. Set it to -1 to disable it.")]
		public readonly int RepairAllBuildingsCoolDown = 107;

		public override object Create(ActorInitializer init) { return new BuildingRepairBotModule(this); }
	}

	public class BuildingRepairBotModule : ConditionalTrait<BuildingRepairBotModuleInfo>, IBotRespondToAttack
	{
		int prevTicks = 0;

		public BuildingRepairBotModule(BuildingRepairBotModuleInfo info)
			: base(info) { }

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			// Check all buildings for repair periodically.
			// We add a RepairAllCoolDown >= 0 for d2k bots to disable it.
			if (Info.RepairAllBuildingsCoolDown >= 0 && prevTicks + Info.RepairAllBuildingsCoolDown < bot.Player.World.WorldTick)
			{
				prevTicks = bot.Player.World.WorldTick;
				var reaprableBuildings = bot.Player.World.ActorsWithTrait<RepairableBuilding>()
					.Where(tp =>
					{
						if (tp.Actor.Owner != bot.Player)
							return false;

						var health = tp.Actor.TraitOrDefault<Health>();
						if (health == null || health.DamageState <= DamageState.Undamaged || health.DamageState == DamageState.Dead)
							return false;

						return !tp.Trait.RepairActive;
					});

				foreach (var tp in reaprableBuildings)
					bot.QueueOrder(new Order("RepairBuilding", self.Owner.PlayerActor, Target.FromActor(tp.Actor), false));

				return;
			}

			// Check if the attacked building needs repair.
			// HACK: We don't want D2k bots to repair all their buildings on placement
			// where half their HP is removed via neutral terrain damage.
			// TODO: Implement concrete placement for D2k bots and remove this hack on players relationship check.
			if (self.IsDead || self.Owner.RelationshipWith(e.Attacker.Owner) == PlayerRelationship.Neutral)
				return;

			var rb = self.TraitOrDefault<RepairableBuilding>();
			if (rb != null && e.DamageState > DamageState.Light && e.PreviousDamageState <= DamageState.Light && !rb.RepairActive)
			{
				AIUtils.BotDebug("{0} noticed damage {1} {2}->{3}, repairing.",
					self.Owner, self, e.PreviousDamageState, e.DamageState);
				bot.QueueOrder(new Order("RepairBuilding", self.Owner.PlayerActor, Target.FromActor(self), false));
			}
		}
	}
}
