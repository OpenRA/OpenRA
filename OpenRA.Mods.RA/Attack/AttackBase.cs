#region Copyright & License Information
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
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public abstract class AttackBaseInfo : ITraitInfo
	{
		[WeaponReference]
		public readonly string PrimaryWeapon = null;
		[WeaponReference]
		public readonly string SecondaryWeapon = null;
		public readonly int PrimaryRecoil = 0;
		public readonly int SecondaryRecoil = 0;
		public readonly float PrimaryRecoilRecovery = 0.2f;
		public readonly float SecondaryRecoilRecovery = 0.2f;
		public readonly int[] PrimaryLocalOffset = { };
		public readonly int[] SecondaryLocalOffset = { };
		public readonly int[] PrimaryOffset = { 0, 0 };
		public readonly int[] SecondaryOffset = null;
		public readonly int FireDelay = 0;

		public readonly bool AlignIdleTurrets = false;
		public readonly bool CanAttackGround = true;

		public readonly int ScanTimeInterval = 50;

		public abstract object Create(ActorInitializer init);

		public float GetMaximumRange()
		{
			var priRange = PrimaryWeapon != null ? Rules.Weapons[PrimaryWeapon.ToLowerInvariant()].Range : 0;
			var secRange = SecondaryWeapon != null ? Rules.Weapons[SecondaryWeapon.ToLowerInvariant()].Range : 0;

			return Math.Max(priRange, secRange);
		}
	}

	public abstract class AttackBase : IIssueOrder, IResolveOrder, ITick, IExplodeModifier, IOrderVoice
	{
		public bool IsAttacking { get; internal set; }

		public List<Weapon> Weapons = new List<Weapon>();
		public List<Turret> Turrets = new List<Turret>();

		readonly Actor self;

		public AttackBase(Actor self)
		{
			this.self = self;
			var info = self.Info.Traits.Get<AttackBaseInfo>();

			Turrets.Add(new Turret(info.PrimaryOffset, info.PrimaryRecoilRecovery));
			if (info.SecondaryOffset != null)
				Turrets.Add(new Turret(info.SecondaryOffset, info.SecondaryRecoilRecovery));

			if (info.PrimaryWeapon != null)
				Weapons.Add(new Weapon(info.PrimaryWeapon,
					Turrets[0], info.PrimaryLocalOffset, info.PrimaryRecoil));

			if (info.SecondaryWeapon != null)
				Weapons.Add(new Weapon(info.SecondaryWeapon,
					info.SecondaryOffset != null ? Turrets[1] : Turrets[0], info.SecondaryLocalOffset, info.SecondaryRecoil));
		}

		protected virtual bool CanAttack(Actor self, Target target)
		{
			if (!self.IsInWorld) return false;
			if (!target.IsValid) return false;
			if (Weapons.All(w => w.IsReloading)) return false;
			if (self.IsDisabled()) return false;

			if (target.IsActor && target.Actor.HasTrait<ITargetable>() &&
				!target.Actor.Trait<ITargetable>().TargetableBy(target.Actor,self))
				return false;

			return true;
		}

		public bool ShouldExplode(Actor self) { return !IsReloading(); }

		public bool IsReloading() { return Weapons.Any(w => w.IsReloading); }

		List<Pair<int, Action>> delayedActions = new List<Pair<int, Action>>();

		public virtual void Tick(Actor self)
		{
			foreach (var w in Weapons)
				w.Tick();

			for (var i = 0; i < delayedActions.Count; i++)
			{
				var x = delayedActions[i];
				if (--x.First <= 0)
					x.Second();
				delayedActions[i] = x;
			}
			delayedActions.RemoveAll(a => a.First <= 0);
		}

		internal void ScheduleDelayedAction(int t, Action a)
		{
			if (t > 0)
				delayedActions.Add(Pair.New(t, a));
			else
				a();
		}

		public virtual void DoAttack(Actor self, Target target)
		{
			if( !CanAttack( self, target ) ) return;

			var move = self.TraitOrDefault<IMove>();
			var facing = self.TraitOrDefault<IFacing>();
			foreach (var w in Weapons)
				w.CheckFire(self, this, move, facing, target);
		}

		public virtual int FireDelay( Actor self, Target target, AttackBaseInfo info )
		{
			return info.FireDelay;
		}

		bool IsHeal { get { return Weapons[ 0 ].Info.Warheads[ 0 ].Damage < 0; } }

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new AttackOrderTargeter( "Attack", 6, IsHeal ); }
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order is AttackOrderTargeter )
			{
				if( target.IsActor )
					return new Order("Attack", self, queued) { TargetActor = target.Actor };
				else
					return new Order( "Attack", self, queued ) { TargetLocation = target.CenterLocation.ToCPos() };
			}
			return null;
		}

		public virtual void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Attack" || order.OrderString == "AttackHold")
			{
				var target = Target.FromOrder(order);
				self.SetTargetLine(target, Color.Red);
				AttackTarget(target, order.Queued, order.OrderString == "Attack");
			}
			else
			{
				/* hack */
				if (self.HasTrait<Turreted>() && self.Info.Traits.Get<AttackBaseInfo>().AlignIdleTurrets)
					self.Trait<Turreted>().desiredFacing = null;
			}
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Attack" || order.OrderString == "AttackHold") ? "Attack" : null;
		}

		public abstract Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove);

		public bool HasAnyValidWeapons(Target t) { return Weapons.Any(w => w.IsValidAgainst(self.World, t)); }
		public float GetMaximumRange() { return Weapons.Max(w => w.Info.Range); }

		public Weapon ChooseWeaponForTarget(Target t) { return Weapons.FirstOrDefault(w => w.IsValidAgainst(self.World, t)); }

		public void AttackTarget( Target target, bool queued, bool allowMove )
		{
			if( !target.IsValid ) return;
			if (!queued) self.CancelActivity();
			self.QueueActivity(GetAttackActivity(self, target, allowMove));
		}

		class AttackOrderTargeter : IOrderTargeter
		{
			readonly bool isHeal;

			public AttackOrderTargeter( string order, int priority, bool isHeal )
			{
				this.OrderID = order;
				this.OrderPriority = priority;
				this.isHeal = isHeal;
			}

			public string OrderID { get; private set; }
			public int OrderPriority { get; private set; }

			public bool CanTargetActor(Actor self, Actor target, bool forceAttack, bool forceQueued, ref string cursor)
			{
				IsQueued = forceQueued;

				cursor = isHeal ? "heal" : "attack";
				if( self == target ) return false;
				if( !self.Trait<AttackBase>().HasAnyValidWeapons( Target.FromActor( target ) ) ) return false;
				if (forceAttack) return true;

				var targetableRelationship = isHeal ? Stance.Ally : Stance.Enemy;

				return self.Owner.Stances[ target.Owner ] == targetableRelationship;
			}

			public bool CanTargetLocation(Actor self, CPos location, List<Actor> actorsAtLocation, bool forceAttack, bool forceQueued, ref string cursor)
			{
				if (!self.World.Map.IsInMap(location))
					return false;

				IsQueued = forceQueued;

				cursor = isHeal ? "heal" : "attack";
				if( isHeal ) return false;
				if( !self.Trait<AttackBase>().HasAnyValidWeapons( Target.FromCell( location ) ) ) return false;

				if( forceAttack )
					if( self.Info.Traits.Get<AttackBaseInfo>().CanAttackGround )
						return true;

				return false;
			}

			public bool IsQueued { get; protected set; }
		}
	}
}
