﻿#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class AttackFollowInfo : AttackBaseInfo
	{
		public override object Create(ActorInitializer init) { return new AttackFollow(init.self, this); }
	}

	public class AttackFollow : AttackBase, ITick, ISync
	{
		public Target Target { get; protected set; }

		public AttackFollow(Actor self, AttackFollowInfo info)
			: base(self, info) { }

		protected override bool CanAttack(Actor self, Target target)
		{
			if (!target.IsValidFor(self))
				return false;

			return base.CanAttack(self, target);
		}

		public virtual void Tick(Actor self)
		{
			DoAttack(self, Target);
			IsAttacking = Target.IsValidFor(self);
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			return new AttackActivity(self, newTarget, allowMove);
		}

		public override void ResolveOrder(Actor self, Order order)
		{
			base.ResolveOrder(self, order);

			if (order.OrderString == "Stop")
				Target = Target.Invalid;
		}

		class AttackActivity : Activity
		{
			readonly AttackFollow attack;
			readonly IMove move;
			readonly Target target;

			public AttackActivity(Actor self, Target target, bool allowMove)
			{
				attack = self.Trait<AttackFollow>();
				move = allowMove ? self.TraitOrDefault<IMove>() : null;

				// HACK: Mobile.OnRails is horrible
				var mobile = move as Mobile;
				if (mobile != null && mobile.Info.OnRails)
					move = null;

				this.target = target;
			}

			public override Activity Tick(Actor self)
			{
				if (IsCanceled || !target.IsValidFor(self))
					return NextActivity;

				if (self.IsDisabled())
					return this;

				var weapon = attack.ChooseArmamentForTarget(target);
				if (weapon != null)
				{
					// Try and sit at least half a cell closer, in case the target starts moving.
					var maxRange = new WRange(Math.Max(0, weapon.Weapon.Range.Range - 512));

					attack.Target = target;

					if (move != null)
						return Util.SequenceActivities(move.MoveFollow(self, target, weapon.Weapon.MinRange, maxRange), this);
				}

				return NextActivity;
			}
		}
	}
}
