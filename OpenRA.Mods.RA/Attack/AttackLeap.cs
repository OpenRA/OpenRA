#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackLeapInfo : AttackFrontalInfo
	{
		[Desc("Leap speed (in units/tick).")]
		public readonly WDist Speed = new WDist(426);
		public readonly WAngle Angle = WAngle.FromDegrees(20);

		public override object Create(ActorInitializer init) { return new AttackLeap(init.self, this); }
	}

	class AttackLeap : AttackFrontal, ISync
	{
		AttackLeapInfo info;

		public AttackLeap(Actor self, AttackLeapInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void DoAttack(Actor self, Target target)
		{
			if (target.Type != TargetType.Actor || !CanAttack(self, target))
				return;

			var a = ChooseArmamentForTarget(target);
			if (a == null)
				return;

			if (!target.IsInRange(self.CenterPosition, a.Weapon.Range))
				return;

			self.CancelActivity();
			self.QueueActivity(new Leap(self, target.Actor, a.Weapon, info.Speed, info.Angle));
		}
	}
}
