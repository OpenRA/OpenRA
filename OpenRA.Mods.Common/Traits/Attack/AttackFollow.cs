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
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor will follow units until in range to attack them.")]
	public class AttackFollowInfo : AttackBaseInfo
	{
		public override object Create(ActorInitializer init) { return new AttackFollow(init.Self, this); }
	}

	public class AttackFollow : AttackBase, INotifyOwnerChanged
	{
		public Target Target { get; protected set; }

		public AttackFollow(Actor self, AttackFollowInfo info)
			: base(self, info) { }

		protected override void Tick(Actor self)
		{
			// We can safely ignore target visibility here - the armament will handle this for us.
			bool targetIsHiddenActor;
			Target = Target.Recalculate(self.Owner, out targetIsHiddenActor);
			if (IsTraitDisabled)
			{
				Target = Target.Invalid;
				return;
			}

			DoAttack(self, Target);
			IsAiming = Target.IsValidFor(self);

			base.Tick(self);
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack)
		{
			return new AttackActivity(self, newTarget, allowMove, forceAttack);
		}

		public override void OnStopOrder(Actor self)
		{
			Target = Target.Invalid;
			base.OnStopOrder(self);
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			Target = Target.Invalid;
		}

		class AttackActivity : Activity
		{
			readonly AttackFollow attack;
			readonly RevealsShroud[] revealsShroud;
			readonly IMove move;
			readonly bool forceAttack;
			Target target;
			bool hasTicked;

			public AttackActivity(Actor self, Target target, bool allowMove, bool forceAttack)
			{
				attack = self.Trait<AttackFollow>();
				move = allowMove ? self.TraitOrDefault<IMove>() : null;
				revealsShroud = self.TraitsImplementing<RevealsShroud>().ToArray();

				this.target = target;
				this.forceAttack = forceAttack;
			}

			public override Activity Tick(Actor self)
			{
				// All of the interesting behaviour to move to the last known target position if it becomes hidden
				// and to reacquire the target if it is revealed enroute is handled inside MoveWithinRange.
				// At this point in the activity chain we are either ticking against the target for the first time
				// (and so don't know where it is), or after MoveWithinRange has lost the target and given up.
				// We can therefore treat a hidden targets as invalid and give up if we can't currently see it.
				target = target.RecalculateInvalidatingHiddenTargets(self.Owner);
				if (IsCanceled || !target.IsValidFor(self))
					return NextActivity;

				if (attack.IsTraitPaused)
					return this;

				var weapon = attack.ChooseArmamentsForTarget(target, forceAttack).FirstOrDefault();
				if (weapon != null)
				{
					// Check that AttackFollow hasn't cancelled the target by modifying attack.Target
					// Having both this and AttackFollow modify that field is a horrible hack.
					if (hasTicked && attack.Target.Type == TargetType.Invalid)
						return NextActivity;

					var targetIsMobile = (target.Type == TargetType.Actor && target.Actor.Info.HasTraitInfo<IMoveInfo>())
						|| (target.Type == TargetType.FrozenActor && target.FrozenActor.Info.HasTraitInfo<IMoveInfo>());

					// Try and sit at least one cell closer than the max range to give some leeway if the target starts moving.
					var modifiedRange = weapon.MaxRange();
					var maxRange = targetIsMobile ? new WDist(Math.Max(weapon.Weapon.MinRange.Length, modifiedRange.Length - 1024))
						: modifiedRange;

					// Most actors want to be able to see their target before shooting
					if (!attack.Info.TargetFrozenActors && !forceAttack && target.Type == TargetType.FrozenActor)
					{
						var rs = revealsShroud
							.Where(Exts.IsTraitEnabled)
							.MaxByOrDefault(s => s.Range);

						// Default to 2 cells if there are no active traits
						var sightRange = rs != null ? rs.Range : WDist.FromCells(2);
						if (sightRange < maxRange)
							maxRange = sightRange;
					}

					attack.Target = target;
					hasTicked = true;

					if (move != null)
						return ActivityUtils.SequenceActivities(
							move.MoveFollow(self, target, weapon.Weapon.MinRange, maxRange, targetLineColor: Color.Red),
							this);

					if (target.IsInRange(self.CenterPosition, weapon.MaxRange()) &&
						!target.IsInRange(self.CenterPosition, weapon.Weapon.MinRange))
						return this;
				}

				attack.Target = Target.Invalid;

				return NextActivity;
			}
		}
	}
}
