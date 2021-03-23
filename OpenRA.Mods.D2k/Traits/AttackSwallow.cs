#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.D2k.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Sandworms use this attack model.")]
	class AttackSwallowInfo : AttackFrontalInfo
	{
		[Desc("The number of ticks it takes to return underground.")]
		public readonly int ReturnDelay = 60;

		[Desc("The number of ticks it takes to get in place under the target to attack.")]
		public readonly int AttackDelay = 30;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while attacking.")]
		public readonly string AttackingCondition = null;

		public readonly string WormAttackSound = "WORM.WAV";

		[NotificationReference("Speech")]
		public readonly string WormAttackNotification = "WormAttack";

		public override object Create(ActorInitializer init) { return new AttackSwallow(init.Self, this); }
	}

	class AttackSwallow : AttackFrontal
	{
		public readonly new AttackSwallowInfo Info;

		public AttackSwallow(Actor self, AttackSwallowInfo info)
			: base(self, info)
		{
			Info = info;
		}

		public override void DoAttack(Actor self, in Target target)
		{
			// This is so that the worm does not launch an attack against a target that has reached solid rock
			if (target.Type != TargetType.Actor || !CanAttack(self, target))
			{
				self.CancelActivity();
				return;
			}

			var a = ChooseArmamentsForTarget(target, true).FirstOrDefault();
			if (a == null)
				return;

			if (!target.IsInRange(self.CenterPosition, a.MaxRange()))
				return;

			self.QueueActivity(false, new SwallowActor(self, target, a, facing));
		}

		public override Activity GetAttackActivity(Actor self, AttackSource source, in Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor)
		{
			return new SwallowTarget(self, newTarget, allowMove, forceAttack);
		}

		public class SwallowTarget : Attack
		{
			public SwallowTarget(Actor self, in Target target, bool allowMovement, bool forceAttack)
				: base(self, target, allowMovement, forceAttack) { }

			protected override Target RecalculateTarget(Actor self, out bool targetIsHiddenActor)
			{
				// Worms ignore visibility, so don't need to recalculate targets
				targetIsHiddenActor = false;
				return target;
			}

			protected override void DoAttack(Actor self, AttackFrontal attack, IEnumerable<Armament> armaments)
			{
				attack.DoAttack(self, target);
			}
		}
	}
}
