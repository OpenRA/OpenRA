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
	// TODO: This is a copy of AttackLeap. Maybe combine them in AttackMelee trait when the code is finalized?
	[Desc("Sandworms use this attack model.")]
	class AttackSwallowInfo : AttackFrontalInfo, Requires<SandwormInfo>
	{
		public override object Create(ActorInitializer init) { return new AttackSwallow(init.self, this); }
	}
	class AttackSwallow : AttackFrontal
	{
		readonly Sandworm sandworm;

		public AttackSwallow(Actor self, AttackSwallowInfo attackSwallowInfo)
			: base(self, attackSwallowInfo)
		{
			sandworm = self.Trait<Sandworm>();
		}

		public override void DoAttack(Actor self, Target target)
		{
			// TODO: Worm should ignore Fremen as targets unless they are firing/being fired upon (even moving fremen do not attract worms)

			if (target.Type != TargetType.Actor || !CanAttack(self, target) || !sandworm.CanAttackAtLocation(self, target.Actor.Location))
				// this is so that the worm does not launch an attack against a target that has reached solid rock
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
			self.QueueActivity(new SwallowActor(self, target.Actor, a.Weapon));
		}
	}
}
