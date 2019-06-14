#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
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
	class AttackSwallowInfo : AttackBaseInfo
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

	class AttackSwallow : AttackBase
	{
		public readonly new AttackSwallowInfo Info;

		public AttackSwallow(Actor self, AttackSwallowInfo info)
			: base(self, info)
		{
			Info = info;
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			if (!base.CanAttack(self, target))
				return false;

			return TargetInFiringArc(self, target, Info.FacingTolerance);
		}

		public override void DoAttack(Actor self, Target target)
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

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor)
		{
			return new SwallowTarget(self, newTarget, allowMove, forceAttack);
		}

		public class SwallowTarget : Activity
		{
			readonly AttackSwallow attack;
			readonly IMove move;
			readonly IPositionable positionable;
			readonly bool forceAttack;

			protected Target target;

			WDist minRange;
			WDist maxRange;

			public SwallowTarget(Actor self, Target target, bool allowMovement, bool forceAttack)
			{
				this.target = target;
				this.forceAttack = forceAttack;

				attack = self.Trait<AttackSwallow>();
				positionable = self.Trait<IPositionable>();

				move = allowMovement ? self.TraitOrDefault<IMove>() : null;
			}

			public override bool Tick(Actor self)
			{
				if (IsCanceling)
					return true;

				if (!target.IsValidFor(self))
				{
					attack.IsAiming = false;
					return true;
				}

				if (attack.Info.AttackRequiresEnteringCell && !positionable.CanEnterCell(target.Actor.Location, null, BlockedByActor.None))
				{
					attack.IsAiming = false;
					return true;
				}

				// Drop the target once none of the weapons are effective against it
				var armaments = attack.ChooseArmamentsForTarget(target, forceAttack).ToList();
				if (armaments.Count == 0)
					return true;

				// Update ranges
				minRange = armaments.Max(a => a.Weapon.MinRange);
				maxRange = armaments.Min(a => a.MaxRange());

				var pos = self.CenterPosition;
				var mobile = move as Mobile;
				if (!target.IsInRange(pos, maxRange)
					|| (minRange.Length != 0 && target.IsInRange(pos, minRange))
					|| (mobile != null && !mobile.CanInteractWithGroundLayer(self)))
				{
					// Try to move within range, drop the target otherwise
					if (move == null)
					{
						attack.IsAiming = false;
						return true;
					}

					QueueChild(move.MoveWithinRange(target, minRange, maxRange, target.CenterPosition));
					attack.IsAiming = false;
					return false;
				}

				if (!attack.TargetInFiringArc(self, target, attack.Info.FacingTolerance))
				{
					var desiredFacing = (attack.GetTargetPosition(pos, target) - pos).Yaw.Facing;
					QueueChild(new Turn(self, desiredFacing));
					attack.IsAiming = true;
					return false;
				}

				attack.DoAttack(self, target);
				attack.IsAiming = true;
				return false;
			}
		}
	}
}
