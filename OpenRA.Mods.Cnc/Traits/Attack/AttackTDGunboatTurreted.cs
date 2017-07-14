#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Actor has a visual turret used to attack.")]
	public class AttackTDGunboatTurretedInfo : AttackTurretedInfo, Requires<TDGunboatInfo>
	{
		public override object Create(ActorInitializer init) { return new AttackTDGunboatTurreted(init.Self, this); }
	}

	public class AttackTDGunboatTurreted : AttackTurreted
	{
		public AttackTDGunboatTurreted(Actor self, AttackTDGunboatTurretedInfo info)
			: base(self, info) { }

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack)
		{
			return new AttackTDGunboatTurretedActivity(self, newTarget, allowMove, forceAttack);
		}

		class AttackTDGunboatTurretedActivity : Activity
		{
			readonly AttackTDGunboatTurreted attack;
			readonly Target target;
			readonly bool forceAttack;
			bool hasTicked;

			public AttackTDGunboatTurretedActivity(Actor self, Target target, bool allowMove, bool forceAttack)
			{
				attack = self.Trait<AttackTDGunboatTurreted>();
				this.target = target;
				this.forceAttack = forceAttack;
			}

			public override Activity Tick(Actor self)
			{
				if (IsCanceled || !target.IsValidFor(self))
					return NextActivity;

				if (attack.IsTraitDisabled)
					return this;

				var weapon = attack.ChooseArmamentsForTarget(target, forceAttack).FirstOrDefault();
				if (weapon != null)
				{
					// Check that AttackTDGunboatTurreted hasn't cancelled the target by modifying attack.Target
					// Having both this and AttackTDGunboatTurreted modify that field is a horrible hack.
					if (hasTicked && attack.Target.Type == TargetType.Invalid)
						return NextActivity;

					attack.Target = target;
					hasTicked = true;
				}

				return NextActivity;
			}
		}
	}
}
