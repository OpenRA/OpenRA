#region Copyright & License Information
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
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackTurretedInfo : AttackBaseInfo, Requires<TurretedInfo>
	{
		public override object Create(ActorInitializer init) { return new AttackTurreted(init.self, this); }
	}

	class AttackTurreted : AttackBase, ITick, INotifyBuildComplete, ISync
	{
		public Target Target { get; protected set; }
		protected IEnumerable<Turreted> turrets;
		[Sync] protected bool buildComplete;

		public AttackTurreted(Actor self, AttackTurretedInfo info)
			: base(self, info)
		{
			turrets = self.TraitsImplementing<Turreted>();
		}

		protected override bool CanAttack(Actor self, Target target)
		{
			if (self.HasTrait<Building>() && !buildComplete)
				return false;

			if (!target.IsValidFor(self))
				return false;

			var canAttack = false;
			foreach (var t in turrets)
				if (t.FaceTarget(self, target))
					canAttack = true;

			if (!canAttack)
				return false;

			return base.CanAttack(self, target);
		}

		public void Tick(Actor self)
		{
			DoAttack(self, Target);
			IsAttacking = Target.IsValidFor(self);
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			return new AttackActivity(newTarget, allowMove);
		}

		public override void ResolveOrder(Actor self, Order order)
		{
			base.ResolveOrder(self, order);

			if (order.OrderString == "Stop")
				Target = Target.Invalid;
		}

		public virtual void BuildingComplete(Actor self) { buildComplete = true; }

		class AttackActivity : Activity
		{
			readonly Target target;
			readonly bool allowMove;

			public AttackActivity(Target newTarget, bool allowMove)
			{
				this.target = newTarget;
				this.allowMove = allowMove;
			}

			public override Activity Tick(Actor self)
			{
				if (IsCanceled || !target.IsValidFor(self))
					return NextActivity;

				if (self.IsDisabled())
					return this;

				var attack = self.Trait<AttackTurreted>();
				const int RangeTolerance = 1;	/* how far inside our maximum range we should try to sit */
				var weapon = attack.ChooseArmamentForTarget(target);

				if (weapon != null)
				{
					var range = WRange.FromCells(Math.Max(0, weapon.Weapon.Range.Range / 1024 - RangeTolerance));

					attack.Target = target;
					var mobile = self.TraitOrDefault<Mobile>();

					if (allowMove && mobile != null && !mobile.Info.OnRails)
						return Util.SequenceActivities(mobile.MoveFollow(self, target, range), this);
				}

				return NextActivity;
			}
		}
	}
}
