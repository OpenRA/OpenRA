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
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public abstract class DamageWarhead : Warhead
	{
		[Desc("How much (raw) damage to deal.")]
		public readonly int Damage = 0;

		[Desc("Types of damage that this warhead causes. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[Desc("Damage percentage versus each armor type.")]
		public readonly Dictionary<string, int> Versus = new Dictionary<string, int>();

		public override bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			// Cannot be damaged without a Health trait
			if (!victim.Info.HasTraitInfo<IHealthInfo>())
				return false;

			return base.IsValidAgainst(victim, firedBy);
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;

			// Used by traits or warheads that damage a single actor, rather than a position
			if (target.Type == TargetType.Actor)
			{
				var victim = target.Actor;

				if (!IsValidAgainst(victim, firedBy))
					return;

				// PERF: Avoid using TraitsImplementing<HitShape> that needs to find the actor in the trait dictionary.
				var closestActiveShape = (HitShape)victim.EnabledTargetablePositions.MinByOrDefault(t =>
				{
					if (t is HitShape h)
						return h.DistanceFromEdge(victim, victim.CenterPosition);
					else
						return WDist.MaxValue;
				});

				// Cannot be damaged without an active HitShape
				if (closestActiveShape == null)
					return;

				InflictDamage(victim, firedBy, closestActiveShape, args);
			}
			else if (target.Type != TargetType.Invalid)
				DoImpact(target.CenterPosition, firedBy, args);
		}

		protected virtual int DamageVersus(Actor victim, HitShape shape, WarheadArgs args)
		{
			// If no Versus values are defined, DamageVersus would return 100 anyway, so we might as well do that early.
			if (Versus.Count == 0)
				return 100;

			var armor = victim.TraitsImplementing<Armor>()
				.Where(a => !a.IsTraitDisabled && a.Info.Type != null && Versus.ContainsKey(a.Info.Type) &&
					(shape.Info.ArmorTypes.IsEmpty || shape.Info.ArmorTypes.Contains(a.Info.Type)))
				.Select(a => Versus[a.Info.Type]);

			return Util.ApplyPercentageModifiers(100, armor);
		}

		protected virtual void InflictDamage(Actor victim, Actor firedBy, HitShape shape, WarheadArgs args)
		{
			var damage = Util.ApplyPercentageModifiers(Damage, args.DamageModifiers.Append(DamageVersus(victim, shape, args)));
			victim.InflictDamage(firedBy, new Damage(damage, DamageTypes));
		}

		protected abstract void DoImpact(WPos pos, Actor firedBy, WarheadArgs args);
	}
}
