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
	class AttackPlaneInfo : AttackFrontalInfo
	{
		public override object Create(ActorInitializer init) { return new AttackPlane(init.self, this); }
	}

	class AttackPlane : AttackFrontal
	{
		public AttackPlane(Actor self, AttackPlaneInfo info) : base(self, info) { }

		protected override void QueueAttack(Actor self, bool queued, Target newTarget)
		{
			target = newTarget;
			self.QueueActivity(queued, new FlyAttack(newTarget));
		}

		protected override bool CanAttack(Actor self)
		{
			// dont fire while landed
			return base.CanAttack(self) 
				&& self.Trait<Aircraft>().Altitude > 0;
		}
	}
}
