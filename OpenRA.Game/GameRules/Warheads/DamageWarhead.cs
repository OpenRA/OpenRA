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
		public readonly string InfDeath = "1";

		[Desc("Whether we should prevent prone response for infantry.")]
		public readonly bool PreventProne = false;

		[Desc("By what percentage should damage be modified against prone infantry.")]
		public readonly int ProneModifier = 50;

		[FieldLoader.LoadUsing("LoadVersus")]
		[Desc("Damage vs each armortype. 0% = can't target.")]
		public readonly Dictionary<string, float> Versus;

		public static object LoadVersus(MiniYaml yaml)
		{
			var nd = yaml.ToDictionary();
			return nd.ContainsKey("Versus")
				? nd["Versus"].ToDictionary(my => FieldLoader.GetValue<float>("(value)", my.Value))
				: new Dictionary<string, float>();
		}

		public override float EffectivenessAgainst(ActorInfo ai)
		{
			var health = ai.Traits.GetOrDefault<HealthInfo>();
			if (health == null)
				return 0f;

			var armor = ai.Traits.GetOrDefault<ArmorInfo>();
			if (armor == null || armor.Type == null)
				return 1f;

			float versus;
			return Versus.TryGetValue(armor.Type, out versus) ? versus : 1f;
		}

		public override void DoImpact(Target target, Actor firedBy, float firepowerModifier)
		{
			// Used by traits that damage a single actor, rather than a position
			if (target.Type == TargetType.Actor)
				DoImpact(target.Actor, firedBy, firepowerModifier);
			else
				DoImpact(target.CenterPosition, firedBy, firepowerModifier);
		}

		public abstract void DoImpact(Actor target, Actor firedBy, float firepowerModifier);
		public abstract void DoImpact(WPos pos, Actor firedBy, float firepowerModifier);
	}
}
