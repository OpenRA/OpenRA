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
		public readonly bool CanAttackGround = true;
		public readonly string Cursor = "attack";

		public abstract object Create(ActorInitializer init);
	}

	public abstract class AttackBase : IIssueOrder, IResolveOrder, ITick, IExplodeModifier, IOrderVoice, ISync
	{
		[Sync] public bool IsAttacking { get; internal set; }

		readonly Actor self;

		Lazy<IEnumerable<Armament>> armaments;
		protected IEnumerable<Armament> Armaments { get { return armaments.Value; } }

		public AttackBase(Actor self)
		{
			this.self = self;
			armaments = Lazy.New(() => self.TraitsImplementing<Armament>());
		}

		protected virtual bool CanAttack(Actor self, Target target)
		{
			if (!self.IsInWorld)
				return false;

			if (!target.IsValid)
				return false;

			if (Armaments.All(a => a.IsReloading))
				return false;

			if (self.IsDisabled())
				return false;

			if (target.IsActor && target.Actor.HasTrait<ITargetable>() &&
				!target.Actor.Trait<ITargetable>().TargetableBy(target.Actor,self))
				return false;

			return true;
		}

		public bool ShouldExplode(Actor self) { return !IsReloading(); }

		public bool IsReloading() { return Armaments.Any(a => a.IsReloading); }

		List<Pair<int, Action>> delayedActions = new List<Pair<int, Action>>();

		public virtual void Tick(Actor self)
		{
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
			foreach (var a in Armaments)
				a.CheckFire(self, this, move, facing, target);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (Armaments.Count() == 0)
					yield break;

				var negativeDamage = (Armaments.First().Weapon.Warheads[0].Damage < 0);

				yield return new AttackOrderTargeter("Attack", 6, negativeDamage);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order is AttackOrderTargeter)
			{
				if (target.IsActor)
					return new Order("Attack", self, queued) { TargetActor = target.Actor };
				else
					return new Order("Attack", self, queued) { TargetLocation = target.CenterLocation.ToCPos() };
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
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Attack" || order.OrderString == "AttackHold") ? "Attack" : null;
		}

		public abstract Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove);

		public bool HasAnyValidWeapons(Target t) { return Armaments.Any(a => a.IsValidAgainst(self.World, t)); }
		public WRange GetMaximumRange() { return new WRange((int)(1024*Armaments.Max(a => a.Weapon.Range))); }

		public Armament ChooseArmamentForTarget(Target t) { return Armaments.FirstOrDefault(a => a.IsValidAgainst(self.World, t)); }

		public void AttackTarget(Target target, bool queued, bool allowMove)
		{
			if (!target.IsValid)
				return;

			if (!queued)
				self.CancelActivity();

			self.QueueActivity(GetAttackActivity(self, target, allowMove));
		}

		class AttackOrderTargeter : IOrderTargeter
		{
			readonly bool negativeDamage;

			public AttackOrderTargeter(string order, int priority, bool negativeDamage)
			{
				this.OrderID = order;
				this.OrderPriority = priority;
				this.negativeDamage = negativeDamage;
			}

			public string OrderID { get; private set; }
			public int OrderPriority { get; private set; }

			public bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				cursor = self.Info.Traits.Get<AttackBaseInfo>().Cursor;

				if (self == target)
					return false;

				if (!self.Trait<AttackBase>().HasAnyValidWeapons(Target.FromActor(target)))
					return false;

				if (modifiers.HasModifier(TargetModifiers.ForceAttack))
					return true;

				if (modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				var targetableRelationship = negativeDamage ? Stance.Ally : Stance.Enemy;

				return self.Owner.Stances[target.Owner] == targetableRelationship;
			}

			public bool CanTargetLocation(Actor self, CPos location, List<Actor> actorsAtLocation, TargetModifiers modifiers, ref string cursor)
			{
				if (!self.World.Map.IsInMap(location))
					return false;

				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				cursor = self.Info.Traits.Get<AttackBaseInfo>().Cursor;

				if (negativeDamage)
					return false;

				if (!self.Trait<AttackBase>().HasAnyValidWeapons(Target.FromCell(location)))
					return false;

				if (modifiers.HasModifier(TargetModifiers.ForceAttack))
					if (self.Info.Traits.Get<AttackBaseInfo>().CanAttackGround)
						return true;

				return false;
			}

			public bool IsQueued { get; protected set; }
		}
	}
}
