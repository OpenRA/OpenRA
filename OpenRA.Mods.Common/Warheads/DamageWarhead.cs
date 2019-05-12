#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
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
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		[Desc("Damage percentage versus each armortype.")]
		public readonly Dictionary<string, int> Versus = new Dictionary<string, int>();

		public override bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			// Cannot be damaged without a Health trait
			if (!victim.Info.HasTraitInfo<IHealthInfo>())
				return false;

			return base.IsValidAgainst(victim, firedBy);
		}

		public int DamageVersus(Actor victim, HitShapeInfo shapeInfo)
		{
			// If no Versus values are defined, DamageVersus would return 100 anyway, so we might as well do that early.
			if (Versus.Count == 0)
				return 100;

			var armor = victim.TraitsImplementing<Armor>()
				.Where(a => !a.IsTraitDisabled && a.Info.Type != null && Versus.ContainsKey(a.Info.Type) &&
					(shapeInfo.ArmorTypes == default(BitSet<ArmorType>) || shapeInfo.ArmorTypes.Contains(a.Info.Type)))
				.Select(a => Versus[a.Info.Type]);

			return Util.ApplyPercentageModifiers(100, armor);
		}

		protected virtual void InflictDamage(Actor victim, Actor firedBy, HitShapeInfo hitshapeInfo, IEnumerable<int> damageModifiers)
		{
			var damage = Util.ApplyPercentageModifiers(Damage, damageModifiers.Append(DamageVersus(victim, hitshapeInfo)));
			victim.InflictDamage(firedBy, new Damage(damage, DamageTypes));
		}

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			// Used by traits or warheads that damage a single actor, rather than a position
			if (target.Type == TargetType.Actor)
			{
				var victim = target.Actor;

				if (!IsValidAgainst(victim, firedBy))
					return;

				var closestActiveShape = victim.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled)
					.MinByOrDefault(t => t.Info.Type.DistanceFromEdge(victim.CenterPosition, victim));

				// Cannot be damaged without an active HitShape
				if (closestActiveShape == null)
					return;

				InflictDamage(victim, firedBy, closestActiveShape.Info, damageModifiers);
			}
			else if (target.Type != TargetType.Invalid)
				DoImpact(target.CenterPosition, firedBy, damageModifiers);
		}

		public abstract void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers);
	}
}
