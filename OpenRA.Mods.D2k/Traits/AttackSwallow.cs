#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.D2k.Activities;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Sandworms use this attack model.")]
	class AttackSwallowInfo : AttackFrontalInfo
	{
		[Desc("The number of ticks it takes to return underground.")]
		public readonly int ReturnTime = 60;

		[Desc("The number of ticks it takes to get in place under the target to attack.")]
		public readonly int AttackTime = 30;

		public readonly string WormAttackNotification = "WormAttack";

		public override object Create(ActorInitializer init) { return new AttackSwallow(init.self, this); }
	}

	class AttackSwallow : AttackFrontal
	{
		public AttackSwallow(Actor self, AttackSwallowInfo info)
			: base(self, info) { }

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
