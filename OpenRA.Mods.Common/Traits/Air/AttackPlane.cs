#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class AttackPlaneInfo : AttackFrontalInfo
	{
		[Desc("Delay, in game ticks, before turning to attack.")]
		public readonly int AttackTurnDelay = 50;

		public override object Create(ActorInitializer init) { return new AttackPlane(init.Self, this); }
	}

	public class AttackPlane : AttackFrontal
	{
		public readonly AttackPlaneInfo AttackPlaneInfo;

		public AttackPlane(Actor self, AttackPlaneInfo info)
			: base(self, info)
		{
			AttackPlaneInfo = info;
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			return new FlyAttack(self, newTarget);
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			// dont fire while landed or when outside the map
			return base.CanAttack(self, target) && self.CenterPosition.Z > 0 && self.World.Map.Contains(self.Location);
		}
	}
}
