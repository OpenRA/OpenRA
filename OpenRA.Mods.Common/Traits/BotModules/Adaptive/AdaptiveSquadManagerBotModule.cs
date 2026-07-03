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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits.BotModules.Squads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Adaptive AI squad manager with fair fog-of-war targeting.")]
	public sealed class AdaptiveSquadManagerBotModuleInfo : SquadManagerBotModuleInfo
	{
		public override object Create(ActorInitializer init) { return new AdaptiveSquadManagerBotModule(init.Self, this); }
	}

	public sealed class AdaptiveSquadManagerBotModule : SquadManagerBotModule
	{
		AdaptiveCommanderModule commander;
		AdaptiveAILobbySettings lobbySettings;

		public AdaptiveSquadManagerBotModule(Actor self, AdaptiveSquadManagerBotModuleInfo info)
			: base(self, info) { }

		protected override void Created(Actor self)
		{
			base.Created(self);
			commander = self.TraitsImplementing<AdaptiveCommanderModule>().FirstOrDefault(t => t.IsTraitEnabled());
			lobbySettings = self.World.WorldActor.TraitOrDefault<AdaptiveAILobbySettings>();
		}

		protected override bool PassesEnemyVisibilityFilter(Actor a) => AdaptiveAIUtils.IsVisibleEnemy(a, Player);

		protected override IEnumerable<Actor> GetProactiveEnemyActors()
		{
			return World.Actors.Where(a => AdaptiveAIUtils.IsVisibleEnemy(a, Player));
		}

		protected override int EffectiveSquadSize
		{
			get
			{
				var baseSize = Info.SquadSize;
				var readiness = commander?.Plan.AttackReadiness ?? 0.5f;
				var aggression = lobbySettings?.CombatAggression ?? 1f;
				var scaled = (int)(baseSize * (1.1f - readiness) * aggression);
				return Exts.Clamp(scaled, 5, baseSize + Info.SquadSizeRandomBonus);
			}
		}

		protected override int EffectiveRushInterval
		{
			get
			{
				var aggression = lobbySettings?.CombatAggression ?? 1f;
				return (int)(Info.RushInterval / aggression);
			}
		}

		protected override bool ShouldAttemptRush()
		{
			if (commander?.Plan.ActiveGoal == AdaptiveGoal.Defend)
				return false;

			var readiness = commander?.Plan.AttackReadiness ?? 0.5f;
			var aggression = lobbySettings?.CombatAggression ?? 1f;
			if (aggression <= 0.65f && readiness < 0.35f)
				return false;

			return true;
		}

		protected override void AfterCleanSquads()
		{
			foreach (var squad in Squads)
			{
				if (squad.TargetActor != null && !AdaptiveAIUtils.IsVisibleEnemy(squad.TargetActor, Player))
					squad.SetActorToTarget(default);

				if (commander?.Plan.ActiveGoal == AdaptiveGoal.Defend && SquadHealthFraction(squad) < 0.35f)
					squad.SetActorToTarget(default);
			}
		}

		static float SquadHealthFraction(Squad squad)
		{
			var maxHp = 0;
			var hp = 0;
			foreach (var unit in squad.Units)
			{
				if (!unit.Info.HasTraitInfo<IHealthInfo>())
					continue;

				var health = unit.Trait<IHealth>();
				maxHp += health.MaxHP;
				hp += health.HP;
			}

			return maxHp == 0 ? 1f : hp / (float)maxHp;
		}
	}
}
