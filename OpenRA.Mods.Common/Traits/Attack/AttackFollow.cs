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

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor will follow units until in range to attack them.")]
	public class AttackFollowInfo : AttackBaseInfo
	{
		public override object Create(ActorInitializer init) { return new AttackFollow(init.Self, this); }
	}

	public class AttackFollow : AttackBase, ITick, INotifyOwnerChanged
	{
		public Target Target { get; protected set; }

		public AttackFollow(Actor self, AttackFollowInfo info)
			: base(self, info) { }

		public virtual void Tick(Actor self)
		{
			if (IsTraitDisabled)
			{
				Target = Target.Invalid;
				return;
			}

			DoAttack(self, Target);
			IsAttacking = Target.IsValidFor(self);
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack)
		{
			return new AttackActivity(self, newTarget, allowMove, forceAttack);
		}

		// OP Mod: made public for Rage generator
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
			readonly AttackFollow[] attackFollows;
			readonly IMove move;
			readonly Target target;
			readonly bool forceAttack;
			bool hasTicked;

			public AttackActivity(Actor self, Target target, bool allowMove, bool forceAttack)
			{
				attackFollows = self.TraitsImplementing<AttackFollow>().ToArray();
				move = allowMove ? self.TraitOrDefault<IMove>() : null;

				this.target = target;
				this.forceAttack = forceAttack;
			}

			Armament GetFirstFeasibleWeapon(Target target, bool forceAttack)
			{
				// We scan attackbases in order.
				// That means, the order of attack base in YAML matters!
				foreach (var atb in attackFollows)
				{
					var weapon = atb.ChooseArmamentsForTarget(target, forceAttack).FirstOrDefault();
					if (weapon != null)
						return weapon;
				}

				return null;
			}

			public override Activity Tick(Actor self)
			{
				if (IsCanceled || !target.IsValidFor(self))
					return NextActivity;

				if (self.IsDisabled())
					return this;

				var weapon = GetFirstFeasibleWeapon(target, forceAttack);
				if (weapon != null)
				{
					var targetIsMobile = (target.Type == TargetType.Actor && target.Actor.Info.HasTraitInfo<IMoveInfo>())
						|| (target.Type == TargetType.FrozenActor && target.FrozenActor.Info.HasTraitInfo<IMoveInfo>());

					// Try and sit at least one cell closer than the max range to give some leeway if the target starts moving.
					var modifiedRange = weapon.MaxRange();
					var maxRange = targetIsMobile ? new WDist(Math.Max(weapon.Weapon.MinRange.Length, modifiedRange.Length - 1024))
						: modifiedRange;

					// Check that AttackFollow hasn't cancelled the target by modifying attack.Target
					// Having both this and AttackFollow modify that field is a horrible hack.
					if (hasTicked && attackFollows.All(a => a.Target.Type == TargetType.Invalid))
						return NextActivity;

					// Assign targets to all so if it can fire, it will fire.
					foreach (var attack in attackFollows)
						attack.Target = target;
					hasTicked = true;

					if (move != null)
						return ActivityUtils.SequenceActivities(move.MoveFollow(self, target, weapon.Weapon.MinRange, maxRange), this);
					if (target.IsInRange(self.CenterPosition, weapon.MaxRange()) &&
						!target.IsInRange(self.CenterPosition, weapon.Weapon.MinRange))
						return this;
				}

				foreach (var attack in attackFollows)
					attack.Target = Target.Invalid;

				return NextActivity;
			}
		}
	}
}
