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
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Periodically damages the actor while it is exposed to vacuum — i.e. it is",
		"NOT protected by a pressurised/sealed condition and (optionally) its oxygen",
		"is depleted. Modelled directly on the engine's DamagedByTerrain trait.")]
	public class DamagedByVacuumInfo : ConditionalTraitInfo, Requires<IHealthInfo>
	{
		[Desc("Damage received per DamageInterval ticks while exposed.")]
		public readonly int Damage = 300;

		[Desc("Delay in ticks between damage applications.")]
		public readonly int DamageInterval = 16;

		[Desc("Damage types used for armour / warhead interaction.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[ConsumedConditionReference]
		[Desc("Boolean condition expression that, while true, protects the unit",
			"(pressurised dome, sealed hull, hazard suit). No damage is taken.")]
		public readonly BooleanExpression SafeCondition = null;

		[Desc("If true, only apply damage once the unit's oxygen is fully depleted.",
			"Requires the Oxygen trait; ignored if absent.")]
		public readonly bool RequireOxygenDepleted = true;

		public override object Create(ActorInitializer init) { return new DamagedByVacuum(this); }
	}

	public class DamagedByVacuum : ConditionalTrait<DamagedByVacuumInfo>, ITick, IObservesVariables
	{
		int ticks;
		bool safe;
		Oxygen oxygen;

		public DamagedByVacuum(DamagedByVacuumInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			base.Created(self);
			oxygen = self.TraitOrDefault<Oxygen>();
		}

		IEnumerable<VariableObserver> IObservesVariables.GetVariableObservers()
		{
			if (Info.SafeCondition != null)
				yield return new VariableObserver(SafeChanged, Info.SafeCondition.Variables);
		}

		void SafeChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			safe = Info.SafeCondition.Evaluate(conditions);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			// Protected by a dome / sealed hull / suit: reset the clock, take no damage.
			if (safe)
			{
				ticks = 0;
				return;
			}

			// Still has air in the tank.
			if (Info.RequireOxygenDepleted && oxygen != null && oxygen.Current > 0)
			{
				ticks = 0;
				return;
			}

			if (--ticks <= 0)
			{
				ticks = Info.DamageInterval;

				// Same call shape as DamagedByTerrain: the world actor is the attacker.
				self.InflictDamage(self.World.WorldActor, new Damage(Info.Damage, Info.DamageTypes));
			}
		}
	}
}
