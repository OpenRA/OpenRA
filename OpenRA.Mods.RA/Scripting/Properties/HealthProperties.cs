#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using Eluant;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("General")]
	public class HealthProperties : ScriptActorProperties, Requires<HealthInfo>
	{
		Health health;
		public HealthProperties(Actor self)
			: base(self)
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

		[Desc("Specifies whether the actor is alive or dead.")]
		public bool IsDead { get { return health.IsDead; } }
	}

	[ScriptPropertyGroup("General")]
	public class InvulnerableProperties : ScriptActorProperties, Requires<ScriptInvulnerableInfo>
	{
		ScriptInvulnerable invulnerable;
		public InvulnerableProperties(Actor self)
			: base(self)
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