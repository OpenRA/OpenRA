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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	[Desc("Manages how AI reacts to its actors receiving damage.")]
	public class AIDamageReactionModuleInfo : IAIModuleInfo
	{
		[Desc("Name for identification purposes.")]
		public readonly string Name = "default-damagereaction-module";

		[Desc("Radius in cells around the base that should be scanned for units to be protected.")]
		public readonly int ProtectUnitScanRadius = 15;

		[Desc("Should the AI repair its buildings if damaged?")]
		public readonly bool ShouldRepairBuildings = true;

		public object Create(ActorInitializer init) { return new AIDamageReactionModule(init.Self, this); }
	}

	public class AIDamageReactionModule : IAIModule, INotifyDamage
	{
		public readonly AIDamageReactionModuleInfo Info;
		HackyAI ai;
		Player player;
		World world;

		public AIDamageReactionModule(Actor self, AIDamageReactionModuleInfo info)
		{
			Info = info;
		}

		string IAIModule.Name { get { return Info.Name; } }

		void IAIModule.Activate(HackyAI ai)
		{
			this.ai = ai;
			player = ai.Player;
			world = player.World;
		}

		void IAIModule.Tick() { }

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (ai == null || !ai.IsEnabled || e.Attacker == null)
				return;

			var attackerStance = e.Attacker.Owner.Stances[self.Owner];
			if (attackerStance == Stance.Neutral || attackerStance == Stance.Ally)
				return;

			DamageReaction(self, e);
		}

		void ProtectOwn(HackyAI ai, Actor attacker)
		{
			var protectSq = ai.GetSquadOfType(SquadType.Protection);
			if (protectSq == null)
				protectSq = ai.RegisterNewSquad(SquadType.Protection, attacker);

			if (!protectSq.IsTargetValid)
				protectSq.TargetActor = attacker;

			if (!protectSq.IsValid)
			{
				var ownUnits = world.FindActorsInCircle(world.Map.CenterOfCell(ai.GetRandomBaseCenter()), WDist.FromCells(Info.ProtectUnitScanRadius))
					.Where(unit => unit.Owner == player && !unit.Info.HasTraitInfo<BuildingInfo>() && !unit.Info.HasTraitInfo<HarvesterInfo>()
						&& unit.Info.HasTraitInfo<AttackBaseInfo>());

				foreach (var a in ownUnits)
					protectSq.Units.Add(a);
			}
		}

		void DamageReaction(Actor self, AttackInfo e)
		{
			var rb = self.TraitOrDefault<RepairableBuilding>();

			if (Info.ShouldRepairBuildings && rb != null)
			{
				if (e.DamageState > DamageState.Light && e.PreviousDamageState <= DamageState.Light && !rb.RepairActive)
				{
					HackyAI.BotDebug("Bot noticed damage {0} {1}->{2}, repairing.", self, e.PreviousDamageState, e.DamageState);
					ai.QueueOrder(new Order("RepairBuilding", self.Owner.PlayerActor, Target.FromActor(self), false));
				}
			}

			if (e.Attacker.Disposed)
				return;

			if (!e.Attacker.Info.HasTraitInfo<ITargetableInfo>())
				return;

			// Protected priority assets, MCVs, harvesters and buildings
			if ((self.Info.HasTraitInfo<HarvesterInfo>() || self.Info.HasTraitInfo<BuildingInfo>() || self.Info.HasTraitInfo<BaseBuildingInfo>()) &&
				player.Stances[e.Attacker.Owner] == Stance.Enemy)
			{
				ai.UpdateDefenseCenter(e.Attacker.Location);
				ProtectOwn(ai, e.Attacker);
			}
		}
	}
}
