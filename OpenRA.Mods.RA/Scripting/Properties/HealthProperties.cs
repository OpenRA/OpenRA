#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("General")]
	public class HealthProperties : ScriptActorProperties, Requires<HealthInfo>
	{
		Health health;
		public HealthProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			health = self.Trait<Health>();
		}

		[Desc("Current health of the actor.")]
		public int Health
		{
			get { return health.HP; }
			set { health.InflictDamage(self, self, health.HP - value, null, true); }
		}

		[Desc("Maximum health of the actor.")]
		public int MaxHealth { get { return health.MaxHP; } }
	}

	[ScriptPropertyGroup("General")]
	public class InvulnerableProperties : ScriptActorProperties, Requires<ScriptInvulnerableInfo>
	{
		ScriptInvulnerable invulnerable;
		public InvulnerableProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			invulnerable = self.Trait<ScriptInvulnerable>();
		}

		[Desc("Set or query unit invulnerablility.")]
		public bool Invulnerable
		{
			get { return invulnerable.Invulnerable; }
			set { invulnerable.Invulnerable = value; }
		}
	}
}