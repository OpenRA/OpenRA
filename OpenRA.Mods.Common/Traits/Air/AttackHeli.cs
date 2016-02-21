#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class AttackHeliInfo : AttackFrontalInfo
	{
		public override object Create(ActorInitializer init) { return new AttackHeli(init.Self, this); }
	}

	public class AttackHeli : AttackFrontal
	{
		public AttackHeli(Actor self, AttackHeliInfo info)
			: base(self, info) { }

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack)
		{
			return new HeliAttack(self, newTarget);
		}
	}
}
