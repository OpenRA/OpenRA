#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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

		public override void ResolveOrder(Actor self, Order order)
		{
			base.ResolveOrder(self, order);

			if (order.OrderString == "Stop")
				Target = Target.Invalid;
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			Target = Target.Invalid;
		}

		class AttackActivity : Activity
		{
			readonly AttackFollow attack;
			readonly IMove move;
			readonly Target target;
			readonly bool forceAttack;
			readonly bool onRailsHack;
			bool hasTicked;

			public AttackActivity(Actor self, Target target, bool allowMove, bool forceAttack)
			{
				attack = self.Trait<AttackFollow>();
				move = allowMove ? self.TraitOrDefault<IMove>() : null;

				// HACK: Mobile.OnRails is horrible. Blergh.
				var mobile = move as Mobile;
				if (mobile != null && mobile.Info.OnRails)
				{
					move = null;
					onRailsHack = true;
				}

				this.target = target;
				this.forceAttack = forceAttack;
			}

			public override Activity Tick(Actor self)
			{
				if (IsCanceled || !target.IsValidFor(self))
					return NextActivity;

				if (self.IsDisabled())
					return this;

				var weapon = attack.ChooseArmamentsForTarget(target, forceAttack).FirstOrDefault();
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
					if (hasTicked && attack.Target.Type == TargetType.Invalid)
						return NextActivity;

					attack.Target = target;
					hasTicked = true;

					if (move != null)
						return ActivityUtils.SequenceActivities(move.MoveFollow(self, target, weapon.Weapon.MinRange, maxRange), this);
					if (!onRailsHack &&
						target.IsInRange(self.CenterPosition, weapon.MaxRange()) &&
						!target.IsInRange(self.CenterPosition, weapon.Weapon.MinRange))
						return this;
				}

				if (!onRailsHack)
					attack.Target = Target.Invalid;

				return NextActivity;
			}
		}
	}
}
