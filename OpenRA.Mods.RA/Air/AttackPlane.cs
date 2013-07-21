﻿#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			return new FlyAttack(newTarget);
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			// dont fire while landed
			return base.CanAttack(self, target) && self.CenterPosition.Z > 0;
		}
	}
}
