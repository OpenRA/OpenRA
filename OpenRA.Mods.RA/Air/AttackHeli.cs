#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	class AttackHeliInfo : AttackFrontalInfo
	{
		public override object Create(ActorInitializer init) { return new AttackHeli(init.self, this); }
	}

	class AttackHeli : AttackFrontal
	{
		public AttackHeli(Actor self, AttackHeliInfo info) : base(self, info) { }

		protected override void QueueAttack(Actor self, Target newTarget)
		{
			target = newTarget;
			self.QueueActivity(new HeliAttack(newTarget));
		}
	}
}
