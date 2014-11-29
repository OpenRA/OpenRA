#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k
{
	[Desc("Sandworms use this attack model.")]
	class AttackSwallowInfo : AttackFrontalInfo
	{
		[Desc("The number of ticks it takes to return underground.")]
		public int ReturnTime = 60;
		[Desc("The number of ticks it takes to get in place under the target to attack.")]
		public int AttackTime = 30;

		public override object Create(ActorInitializer init) { return new AttackSwallow(init.self, this); }
	}

	class AttackSwallow : AttackFrontal
	{
		public readonly AttackSwallowInfo AttackSwallowInfo;

		public AttackSwallow(Actor self, AttackSwallowInfo attackSwallowInfo)
			: base(self, attackSwallowInfo)
		{
			AttackSwallowInfo = attackSwallowInfo;
		}

		public override void DoAttack(Actor self, Target target)
		{
			// This is so that the worm does not launch an attack against a target that has reached solid rock
			if (target.Type != TargetType.Actor || !CanAttack(self, target))
			{
				self.CancelActivity();
				return;
			}

			var a = ChooseArmamentForTarget(target);
			if (a == null)
				return;

			if (!target.IsInRange(self.CenterPosition, a.Weapon.Range))
				return;

			self.CancelActivity();
			self.QueueActivity(new SwallowActor(self, target, a.Weapon));
		}
	}
}
