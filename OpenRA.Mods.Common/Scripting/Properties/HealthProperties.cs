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

using Eluant;
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("General")]
	public class HealthProperties : ScriptActorProperties, Requires<IHealthInfo>
	{
		readonly IHealth health;
		public HealthProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			health = self.Trait<IHealth>();
		}

		[Desc("Current health of the actor.")]
		public int Health
		{
			get => health.HP;
			set => health.InflictDamage(Self, Self, new Damage(health.HP - value), true);
		}

		[Desc("Maximum health of the actor.")]
		public int MaxHealth => health.MaxHP;

		[Desc("Kill the actor. damageTypes may be omitted, specified as a string, or as table of strings.")]
		public void Kill(object damageTypes = null)
		{
			Damage damage;
			if (damageTypes is string d)
				damage = new Damage(health.MaxHP, new BitSet<DamageType>(new[] { d }));
			else if (damageTypes is LuaTable t && t.TryGetClrValue(out string[] ds))
				damage = new Damage(health.MaxHP, new BitSet<DamageType>(ds));
			else
				damage = new Damage(health.MaxHP);

			health.InflictDamage(Self, Self, damage, true);
		}
	}
}
