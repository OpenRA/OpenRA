﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackTurretedInfo : AttackBaseInfo, Requires<TurretedInfo>
	{
		public override object Create(ActorInitializer init) { return new AttackTurreted( init.self ); }
	}

	class AttackTurreted : AttackBase, INotifyBuildComplete, ISync
	{
		protected Target target;
		protected IEnumerable<Turreted> turrets;
		[Sync] protected bool buildComplete;

		public AttackTurreted(Actor self) : base(self)
		{
			turrets = self.TraitsImplementing<Turreted>();
		}

		protected override bool CanAttack( Actor self, Target target )
		{
			if( self.HasTrait<Building>() && !buildComplete )
				return false;

			if (!target.IsValid) return false;

			bool canAttack = false;
			foreach (var t in turrets)
				if (t.FaceTarget(self, target))
					canAttack = true;
			if (!canAttack)	return false;

			return base.CanAttack( self, target );
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);
			DoAttack( self, target );
			IsAttacking = target.IsValid;
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			return new AttackActivity( newTarget, allowMove );
		}

		public override void ResolveOrder(Actor self, Order order)
		{
			base.ResolveOrder(self, order);

			if (order.OrderString == "Stop")
				target = Target.None;
		}

		public virtual void BuildingComplete(Actor self) { buildComplete = true; }

		class AttackActivity : Activity
		{
			readonly Target target;
			readonly bool allowMove;

			public AttackActivity( Target newTarget, bool allowMove )
			{
				this.target = newTarget;
				this.allowMove = allowMove;
			}

			public override Activity Tick( Actor self )
			{
				if( IsCanceled || !target.IsValid ) return NextActivity;

				if (self.IsDisabled()) return this;

				var attack = self.Trait<AttackTurreted>();
				const int RangeTolerance = 1;	/* how far inside our maximum range we should try to sit */
				var weapon = attack.ChooseArmamentForTarget(target);

				if (weapon != null)
				{
					attack.target = target;

					if (allowMove && self.HasTrait<Mobile>() && !self.Info.Traits.Get<MobileInfo>().OnRails)
						return Util.SequenceActivities(
							new Follow( target, Math.Max( 0, (int)weapon.Weapon.Range - RangeTolerance ) ),
							this );
				}

				return NextActivity;
			}
		}
	}
}
