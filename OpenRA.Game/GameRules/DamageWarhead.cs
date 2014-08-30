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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.GameRules
{
	public abstract class DamageWarhead : Warhead
	{
		[Desc("How much (raw) damage to deal")]
		public readonly int Damage = 0;

		[Desc("Infantry death animation to use")]
		public readonly string DeathType = "1";

		[Desc("Whether we should prevent prone response for infantry.")]
		public readonly bool PreventProne = false;

		[Desc("By what percentage should damage be modified against prone infantry.")]
		public readonly int ProneModifier = 50;

		[FieldLoader.LoadUsing("LoadVersus")]
		[Desc("Damage percentage versus each armortype. 0% = can't target.")]
		public readonly Dictionary<string, int> Versus;

		public static object LoadVersus(MiniYaml yaml)
		{
			var nd = yaml.ToDictionary();
			return nd.ContainsKey("Versus")
				? nd["Versus"].ToDictionary(my => FieldLoader.GetValue<int>("(value)", my.Value))
				: new Dictionary<string, int>();
		}

		public int DamageVersus(ActorInfo victim)
		{
			var armor = victim.Traits.GetOrDefault<ArmorInfo>();
			if (armor != null && armor.Type != null)
			{
				int versus;
				if (Versus.TryGetValue(armor.Type, out versus))
					return versus;
			}

			return 100;
		}

		// TODO: This can be removed after the legacy and redundant 0% = not targetable
		// assumption has been removed from the yaml definitions
		public override bool CanTargetActor(ActorInfo victim, Actor firedBy)
		{
			var health = victim.Traits.GetOrDefault<HealthInfo>();
			if (health == null)
				return false;

			return DamageVersus(victim) > 0;
		}

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			// Used by traits that damage a single actor, rather than a position
			if (target.Type == TargetType.Actor)
				DoImpact(target.Actor, firedBy, damageModifiers);
			else
				DoImpact(target.CenterPosition, firedBy, damageModifiers);
		}

		public abstract void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers);

		public virtual void DoImpact(Actor victim, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var damage = Util.ApplyPercentageModifiers(Damage, damageModifiers.Append(DamageVersus(victim.Info)));
			victim.InflictDamage(firedBy, damage, this);
		}
	}
}
