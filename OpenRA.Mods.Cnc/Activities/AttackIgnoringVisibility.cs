#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{
	/* Attack against targets that might not be visible (i.e. Hunter-Seeker) */
	public class AttackIgnoringVisibility : Activity, IActivityNotifyStanceChanged
	{
		[Flags]
		protected enum AttackStatus { UnableToAttack, NeedsToTurn, NeedsToMove, Attacking }

		readonly IEnumerable<AttackBase> attackTraits;
		readonly IMove move;
		readonly Mobile mobile;
		readonly IFacing facing;
		readonly IPositionable positionable;

		protected Target target;

		WDist minRange;
		WDist maxRange;
		AttackStatus attackStatus = AttackStatus.UnableToAttack;

		public AttackIgnoringVisibility(Actor self, in Target target)
		{
			this.target = target;

			attackTraits = self.TraitsImplementing<AttackBase>().ToArray().Where(t => !t.IsTraitDisabled);
			facing = self.Trait<IFacing>();
			positionable = self.Trait<IPositionable>();

			var iMove = self.TraitOrDefault<IMove>();
			mobile = iMove as Mobile;
			move = iMove;
		}

		protected virtual Target RecalculateTarget(Actor self)
		{
			var t = target;
			if (t.Type == TargetType.Invalid && t.Actor != null && t.Actor.ReplacedByActor != null)
				t = Target.FromActor(t.Actor.ReplacedByActor);
			return t;
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			if (!attackTraits.Any())
			{
				Cancel(self);
				return false;
			}

			target = RecalculateTarget(self);

			attackStatus = AttackStatus.UnableToAttack;

			foreach (var attack in attackTraits)
			{
				var status = TickAttack(self, attack);
				attack.IsAiming = status == AttackStatus.Attacking || status == AttackStatus.NeedsToTurn;
			}

			if (attackStatus >= AttackStatus.NeedsToTurn)
				return false;

			return true;
		}

		protected override void OnLastRun(Actor self)
		{
			foreach (var attack in attackTraits)
				attack.IsAiming = false;
		}

		protected virtual AttackStatus TickAttack(Actor self, AttackBase attack)
		{
			if (!target.IsValidFor(self))
				return AttackStatus.UnableToAttack;

			if (attack.Info.AttackRequiresEnteringCell && !positionable.CanEnterCell(target.Actor.Location, null, BlockedByActor.None))
				return AttackStatus.UnableToAttack;

			// Drop the target once none of the weapons are effective against it
			var armaments = attack.ChooseArmamentsForTarget(target, false).ToList();
			if (armaments.Count == 0)
				return AttackStatus.UnableToAttack;

			// Update ranges. Exclude paused armaments except when ALL weapons are paused
			// (e.g. out of ammo), in which case use the paused, valid weapon with highest range.
			var activeArmaments = armaments.Where(x => !x.IsTraitPaused).ToList();
			if (activeArmaments.Count != 0)
			{
				minRange = activeArmaments.Max(a => a.Weapon.MinRange);
				maxRange = activeArmaments.Min(a => a.MaxRange());
			}
			else
			{
				minRange = WDist.Zero;
				maxRange = armaments.Max(a => a.MaxRange());
			}

			var pos = self.CenterPosition;
			if (!target.IsInRange(pos, maxRange)
				|| (minRange.Length != 0 && target.IsInRange(pos, minRange))
				|| (mobile != null && !mobile.CanInteractWithGroundLayer(self)))
			{
				// Try to move within range, drop the target otherwise
				if (move == null)
					return AttackStatus.UnableToAttack;

				attackStatus |= AttackStatus.NeedsToMove;
				var checkTarget = target;
				QueueChild(move.MoveWithinRange(target, minRange, maxRange, checkTarget.CenterPosition, Color.Red));
				return AttackStatus.NeedsToMove;
			}

			if (!attack.TargetInFiringArc(self, target, attack.Info.FacingTolerance))
			{
				// Mirror Turn activity checks.
				if (mobile == null || (!mobile.IsTraitDisabled && !mobile.IsTraitPaused))
				{
					// Don't queue a Turn activity: Executing a child takes an additional tick during which the target may have moved again.
					facing.Facing = Common.Util.TickFacing(facing.Facing, (attack.GetTargetPosition(pos, target) - pos).Yaw, facing.TurnSpeed);

					// Check again if we turned enough and directly continue attacking if we did.
					if (!attack.TargetInFiringArc(self, target, attack.Info.FacingTolerance))
					{
						attackStatus |= AttackStatus.NeedsToTurn;
						return AttackStatus.NeedsToTurn;
					}
				}
				else
				{
					attackStatus |= AttackStatus.NeedsToTurn;
					return AttackStatus.NeedsToTurn;
				}
			}

			attackStatus |= AttackStatus.Attacking;
			DoAttack(self, attack, armaments);

			return AttackStatus.Attacking;
		}

		protected virtual void DoAttack(Actor self, AttackBase attack, IEnumerable<Armament> armaments)
		{
			if (!attack.IsTraitPaused)
				foreach (var a in armaments)
					a.CheckFire(self, facing, target, false);
		}

		void IActivityNotifyStanceChanged.StanceChanged(Actor self, AutoTarget autoTarget, UnitStance oldStance, UnitStance newStance)
		{
			// Cancel non-forced targets when switching to a more restrictive stance if they are no longer valid for auto-targeting
			if (newStance > oldStance)
				return;
		}
	}
}
