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
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public abstract class AttackBaseInfo : ITraitInfo
	{
		[Desc("Armament names")]
		public readonly string[] Armaments = { "primary", "secondary" };

		public readonly string Cursor = "attack";
		public readonly string OutsideRangeCursor = "attackoutsiderange";

		public abstract object Create(ActorInitializer init);
	}

	public abstract class AttackBase : IIssueOrder, IResolveOrder, IOrderVoice, ISync
	{
		[Sync] public bool IsAttacking { get; internal set; }
		public IEnumerable<Armament> Armaments { get { return GetArmaments(); } }
		protected Lazy<IFacing> facing;
		protected Lazy<Building> building;
		protected Func<IEnumerable<Armament>> GetArmaments;

		readonly Actor self;
		readonly AttackBaseInfo info;

		public AttackBase(Actor self, AttackBaseInfo info)
		{
			this.self = self;
			this.info = info;

			var armaments = Exts.Lazy(() => self.TraitsImplementing<Armament>()
				.Where(a => info.Armaments.Contains(a.Info.Name)));

			GetArmaments = () => armaments.Value;

			facing = Exts.Lazy(() => self.TraitOrDefault<IFacing>());
			building = Exts.Lazy(() => self.TraitOrDefault<Building>());
		}

		protected virtual bool CanAttack(Actor self, Target target)
		{
			if (!self.IsInWorld)
				return false;

			// Building is under construction or is being sold
			if (building.Value != null && !building.Value.BuildComplete)
				return false;

			if (!target.IsValidFor(self))
				return false;

			if (Armaments.All(a => a.IsReloading))
				return false;

			if (self.IsDisabled())
				return false;

			return true;
		}

		public virtual void DoAttack(Actor self, Target target)
		{
			if (!CanAttack(self, target))
				return;

			foreach (var a in Armaments)
				a.CheckFire(self, facing.Value, target);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (!Armaments.Any())
					yield break;

				var negativeDamage = Armaments.First().Weapon.Warheads[0].Damage < 0;
				yield return new AttackOrderTargeter(this, "Attack", 6, negativeDamage);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order is AttackOrderTargeter)
			{
				switch (target.Type)
				{
				case TargetType.Actor:
					return new Order("Attack", self, queued) { TargetActor = target.Actor };
				case TargetType.FrozenActor:
					return new Order("Attack", self, queued) { ExtraData = target.FrozenActor.ID };
				case TargetType.Terrain:
					return new Order("Attack", self, queued) { TargetLocation = target.CenterPosition.ToCPos() };
				}
			}

			return null;
		}

		public virtual void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Attack")
			{
				var target = self.ResolveFrozenActorOrder(order, Color.Red);
				if (!target.IsValidFor(self))
					return;

				self.SetTargetLine(target, Color.Red);
				AttackTarget(target, order.Queued, true);
			}
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Attack" ? "Attack" : null;
		}

		public abstract Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove);

		public bool HasAnyValidWeapons(Target t) { return Armaments.Any(a => a.Weapon.IsValidAgainst(t, self.World)); }
		public WRange GetMaximumRange()
		{
			if (!Armaments.Any())
				return WRange.Zero;

			return Armaments.Max(a => a.Weapon.Range);
		}

		public Armament ChooseArmamentForTarget(Target t) { return Armaments.FirstOrDefault(a => a.Weapon.IsValidAgainst(t, self.World)); }

		public void AttackTarget(Target target, bool queued, bool allowMove)
		{
			if (!target.IsValidFor(self))
				return;

			if (!queued)
				self.CancelActivity();

			self.QueueActivity(GetAttackActivity(self, target, allowMove));
		}

		public bool IsReachableTarget(Target target, bool allowMove)
		{
			return HasAnyValidWeapons(target)
				&& (target.IsInRange(self.CenterPosition, GetMaximumRange()) || (self.HasTrait<IMove>() && allowMove));
		}

		class AttackOrderTargeter : IOrderTargeter
		{
			readonly bool negativeDamage;
			readonly AttackBase ab;

			public AttackOrderTargeter(AttackBase ab, string order, int priority, bool negativeDamage)
			{
				this.ab = ab;
				this.OrderID = order;
				this.OrderPriority = priority;
				this.negativeDamage = negativeDamage;
			}

			public string OrderID { get; private set; }
			public int OrderPriority { get; private set; }

			bool CanTargetActor(Actor self, Target target, TargetModifiers modifiers, ref string cursor)
			{
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				var a = ab.ChooseArmamentForTarget(target);
				cursor = a != null && !target.IsInRange(self.CenterPosition, a.Weapon.Range)
					? ab.info.OutsideRangeCursor
					: ab.info.Cursor;

				if (target.Type == TargetType.Actor && target.Actor == self)
					return false;

				if (!ab.HasAnyValidWeapons(target))
					return false;

				if (modifiers.HasModifier(TargetModifiers.ForceAttack))
					return true;

				if (modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				if (target.RequiresForceFire)
					return false;

				var targetableRelationship = negativeDamage ? Stance.Ally : Stance.Enemy;

				var owner = target.Type == TargetType.FrozenActor ? target.FrozenActor.Owner : target.Actor.Owner;
				return self.Owner.Stances[owner] == targetableRelationship;
			}

			bool CanTargetLocation(Actor self, CPos location, List<Actor> actorsAtLocation, TargetModifiers modifiers, ref string cursor)
			{
				if (!self.World.Map.IsInMap(location))
					return false;

				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				cursor = ab.info.Cursor;

				if (negativeDamage)
					return false;

				if (!ab.HasAnyValidWeapons(Target.FromCell(location)))
					return false;

				if (modifiers.HasModifier(TargetModifiers.ForceAttack))
				{
					var maxRange = ab.GetMaximumRange().Range;
					var targetRange = (location.CenterPosition - self.CenterPosition).HorizontalLengthSquared;
					if (targetRange > maxRange * maxRange)
						cursor = ab.info.OutsideRangeCursor;

					return true;
				}

				return false;
			}

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, TargetModifiers modifiers, ref string cursor)
			{
				switch (target.Type)
				{
				case TargetType.Actor:
				case TargetType.FrozenActor:
					return CanTargetActor(self, target, modifiers, ref cursor);
				case TargetType.Terrain:
					return CanTargetLocation(self, target.CenterPosition.ToCPos(), othersAtTarget, modifiers, ref cursor);
				default:
					return false;
				}
			}

			public bool IsQueued { get; protected set; }
		}
	}
}
