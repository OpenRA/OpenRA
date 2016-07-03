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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class AttackBaseInfo : UpgradableTraitInfo
	{
		[Desc("Armament names")]
		public readonly string[] Armaments = { "primary", "secondary" };

		public readonly string Cursor = null;

		public readonly string OutsideRangeCursor = null;

		[Desc("Does the attack type require the attacker to enter the target's cell?")]
		public readonly bool AttackRequiresEnteringCell = false;

		[Desc("Does not care about shroud or fog. Enables the actor to launch an attack against a target even if he has no visibility of it.")]
		public readonly bool IgnoresVisibility = false;

		[VoiceReference] public readonly string Voice = "Action";

		public override abstract object Create(ActorInitializer init);
	}

	public abstract class AttackBase : UpgradableTrait<AttackBaseInfo>, IIssueOrder, IResolveOrder, IOrderVoice, ISync
	{
		readonly string attackOrderName = "Attack";
		readonly string forceAttackOrderName = "ForceAttack";

		[Sync] public bool IsAttacking { get; internal set; }
		public IEnumerable<Armament> Armaments { get { return getArmaments(); } }

		protected Lazy<IFacing> facing;
		protected Lazy<Building> building;
		protected Lazy<IPositionable> positionable;
		protected Func<IEnumerable<Armament>> getArmaments;

		readonly Actor self;

		public AttackBase(Actor self, AttackBaseInfo info)
			: base(info)
		{
			this.self = self;

			var armaments = Exts.Lazy(() => self.TraitsImplementing<Armament>()
				.Where(a => info.Armaments.Contains(a.Info.Name)).ToArray());

			getArmaments = () => armaments.Value;

			facing = Exts.Lazy(() => self.TraitOrDefault<IFacing>());
			building = Exts.Lazy(() => self.TraitOrDefault<Building>());
			positionable = Exts.Lazy(() => self.Trait<IPositionable>());
		}

		protected virtual bool CanAttack(Actor self, Target target)
		{
			if (!self.IsInWorld || IsTraitDisabled || self.IsDisabled())
				return false;

			if (!target.IsValidFor(self))
				return false;

			if (!HasAnyValidWeapons(target))
				return false;

			// Building is under construction or is being sold
			if (building.Value != null && !building.Value.BuildComplete)
				return false;

			if (Armaments.All(a => a.IsReloading))
				return false;

			return true;
		}

		public virtual void DoAttack(Actor self, Target target, IEnumerable<Armament> armaments = null)
		{
			if (armaments == null && !CanAttack(self, target))
				return;

			foreach (var a in armaments ?? Armaments)
				a.CheckFire(self, facing.Value, target);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				var armament = Armaments.FirstOrDefault(a => a.Weapon.Warheads.Any(w => (w is DamageWarhead)));
				if (armament == null)
					yield break;

				var negativeDamage = (armament.Weapon.Warheads.FirstOrDefault(w => (w is DamageWarhead)) as DamageWarhead).Damage < 0;
				yield return new AttackOrderTargeter(this, 6, negativeDamage);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order is AttackOrderTargeter)
			{
				switch (target.Type)
				{
					case TargetType.Actor:
						return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
					case TargetType.FrozenActor:
						return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };
					case TargetType.Terrain:
						return new Order(order.OrderID, self, queued) { TargetLocation = self.World.Map.CellContaining(target.CenterPosition) };
				}
			}

			return null;
		}

		public virtual void ResolveOrder(Actor self, Order order)
		{
			var forceAttack = order.OrderString == forceAttackOrderName;
			if (forceAttack || order.OrderString == attackOrderName)
			{
				var target = self.ResolveFrozenActorOrder(order, Color.Red);
				if (!target.IsValidFor(self))
					return;

				self.SetTargetLine(target, Color.Red);
				AttackTarget(target, order.Queued, true, forceAttack);
			}

			if (order.OrderString == "Stop")
				self.CancelActivity();
		}

		static Target TargetFromOrder(Actor self, Order order)
		{
			// Not targeting a frozen actor
			if (order.ExtraData == 0)
				return Target.FromOrder(self.World, order);

			// Targeted an actor under the fog
			var frozenLayer = self.Owner.PlayerActor.TraitOrDefault<FrozenActorLayer>();
			if (frozenLayer == null)
				return Target.Invalid;

			var frozen = frozenLayer.FromID(order.ExtraData);
			if (frozen == null)
				return Target.Invalid;

			// Target is still alive - resolve the real order
			if (frozen.Actor != null && frozen.Actor.IsInWorld)
				return Target.FromActor(frozen.Actor);

			return Target.Invalid;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == attackOrderName || order.OrderString == forceAttackOrderName ? Info.Voice : null;
		}

		public abstract Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack);

		public bool HasAnyValidWeapons(Target t)
		{
			if (IsTraitDisabled)
				return false;

			if (Info.AttackRequiresEnteringCell && !positionable.Value.CanEnterCell(t.Actor.Location, null, false))
				return false;

			// PERF: Avoid LINQ.
			foreach (var armament in Armaments)
				if (!armament.OutOfAmmo && armament.Weapon.IsValidAgainst(t, self.World, self))
					return true;

			return false;
		}

		public WDist GetMinimumRange()
		{
			if (IsTraitDisabled)
				return WDist.Zero;

			// PERF: Avoid LINQ.
			var min = WDist.MaxValue;
			foreach (var armament in Armaments)
			{
				if (armament.IsTraitDisabled)
					continue;

				if (armament.OutOfAmmo)
					continue;

				var range = armament.Weapon.MinRange;
				if (min > range)
					min = range;
			}

			return min != WDist.MaxValue ? min : WDist.Zero;
		}

		public WDist GetMaximumRange()
		{
			if (IsTraitDisabled)
				return WDist.Zero;

			// PERF: Avoid LINQ.
			var max = WDist.Zero;
			foreach (var armament in Armaments)
			{
				if (armament.IsTraitDisabled)
					continue;

				if (armament.OutOfAmmo)
					continue;

				var range = armament.MaxRange();
				if (max < range)
					max = range;
			}

			return max;
		}

		public WDist GetMinimumRangeVersusTarget(Target target)
		{
			if (IsTraitDisabled)
				return WDist.Zero;

			// PERF: Avoid LINQ.
			var min = WDist.MaxValue;
			foreach (var armament in Armaments)
			{
				if (armament.IsTraitDisabled)
					continue;

				if (armament.OutOfAmmo)
					continue;

				if (!armament.Weapon.IsValidAgainst(target, self.World, self))
					continue;

				var range = armament.Weapon.MinRange;
				if (min > range)
					min = range;
			}

			return min != WDist.MaxValue ? min : WDist.Zero;
		}

		public WDist GetMaximumRangeVersusTarget(Target target)
		{
			if (IsTraitDisabled)
				return WDist.Zero;

			// PERF: Avoid LINQ.
			var max = WDist.Zero;
			foreach (var armament in Armaments)
			{
				if (armament.IsTraitDisabled)
					continue;

				if (armament.OutOfAmmo)
					continue;

				if (!armament.Weapon.IsValidAgainst(target, self.World, self))
					continue;

				var range = armament.MaxRange();
				if (max < range)
					max = range;
			}

			return max;
		}

		// Enumerates all armaments, that this actor possesses, that can be used against Target t
		public IEnumerable<Armament> ChooseArmamentsForTarget(Target t, bool forceAttack)
		{
			// If force-fire is not used, and the target requires force-firing or the target is
			// terrain or invalid, no armaments can be used
			if (!forceAttack && (t.Type == TargetType.Terrain || t.Type == TargetType.Invalid || t.RequiresForceFire))
				return Enumerable.Empty<Armament>();

			// Get target's owner; in case of terrain or invalid target there will be no problems
			// with owner == null since forceFire will have to be true in this part of the method
			// (short-circuiting in the logical expression below)
			Player owner = null;
			if (t.Type == TargetType.FrozenActor)
			{
				owner = t.FrozenActor.Owner;
			}
			else if (t.Type == TargetType.Actor)
			{
				owner = t.Actor.EffectiveOwner != null && t.Actor.EffectiveOwner.Owner != null
					? t.Actor.EffectiveOwner.Owner
					: t.Actor.Owner;

				// Special cases for spies so we don't kill friendly disguised spies
				// and enable dogs to kill enemy disguised spies.
				if (self.Owner.Stances[t.Actor.Owner] == Stance.Ally || self.Info.HasTraitInfo<IgnoresDisguiseInfo>())
					owner = t.Actor.Owner;
			}

			return Armaments.Where(a =>
				!a.IsTraitDisabled
				&& (owner == null || (forceAttack ? a.Info.ForceTargetStances : a.Info.TargetStances)
					.HasStance(self.Owner.Stances[owner]))
				&& a.Weapon.IsValidAgainst(t, self.World, self));
		}

		public void AttackTarget(Target target, bool queued, bool allowMove, bool forceAttack = false)
		{
			if (self.IsDisabled() || IsTraitDisabled)
				return;

			if (!target.IsValidFor(self))
				return;

			if (!queued)
				self.CancelActivity();

			self.QueueActivity(GetAttackActivity(self, target, allowMove, forceAttack));
		}

		public bool IsReachableTarget(Target target, bool allowMove)
		{
			return HasAnyValidWeapons(target)
				&& (target.IsInRange(self.CenterPosition, GetMaximumRangeVersusTarget(target)) || (allowMove && self.Info.HasTraitInfo<IMoveInfo>()));
		}

		public Stance UnforcedAttackTargetStances()
		{
			// PERF: Avoid LINQ.
			var stances = Stance.None;
			foreach (var armament in Armaments)
				if (!armament.IsTraitDisabled)
					stances |= armament.Info.TargetStances;

			return stances;
		}

		class AttackOrderTargeter : IOrderTargeter
		{
			readonly AttackBase ab;

			public AttackOrderTargeter(AttackBase ab, int priority, bool negativeDamage)
			{
				this.ab = ab;
				OrderID = ab.attackOrderName;
				OrderPriority = priority;
			}

			public string OrderID { get; private set; }
			public int OrderPriority { get; private set; }
			public bool TargetOverridesSelection(TargetModifiers modifiers) { return true; }

			bool CanTargetActor(Actor self, Target target, ref TargetModifiers modifiers, ref string cursor)
			{
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				if (modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				// Disguised actors are revealed by the attack cursor
				// HACK: works around limitations in the targeting code that force the
				// targeting and attacking logic (which should be logically separate)
				// to use the same code
				if (target.Type == TargetType.Actor && target.Actor.EffectiveOwner != null &&
						target.Actor.EffectiveOwner.Disguised && self.Owner.Stances[target.Actor.Owner] == Stance.Enemy)
					modifiers |= TargetModifiers.ForceAttack;

				var forceAttack = modifiers.HasModifier(TargetModifiers.ForceAttack);
				var armaments = ab.ChooseArmamentsForTarget(target, forceAttack);
				if (!armaments.Any())
					return false;

				// Use valid armament with highest range out of those that have ammo
				// If all are out of ammo, just use valid armament with highest range
				armaments = armaments.OrderByDescending(x => x.MaxRange());
				var a = armaments.FirstOrDefault(x => !x.OutOfAmmo);
				if (a == null)
					a = armaments.First();

				cursor = !target.IsInRange(self.CenterPosition, a.MaxRange())
					? ab.Info.OutsideRangeCursor ?? a.Info.OutsideRangeCursor
					: ab.Info.Cursor ?? a.Info.Cursor;

				if (!forceAttack)
					return true;

				OrderID = ab.forceAttackOrderName;
				return true;
			}

			bool CanTargetLocation(Actor self, CPos location, List<Actor> actorsAtLocation, TargetModifiers modifiers, ref string cursor)
			{
				if (!self.World.Map.Contains(location))
					return false;

				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				// Targeting the terrain is only possible with force-attack modifier
				if (modifiers.HasModifier(TargetModifiers.ForceMove) || !modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;

				var target = Target.FromCell(self.World, location);
				var armaments = ab.ChooseArmamentsForTarget(target, true);
				if (!armaments.Any())
					return false;

				// Use valid armament with highest range out of those that have ammo
				// If all are out of ammo, just use valid armament with highest range
				armaments = armaments.OrderByDescending(x => x.MaxRange());
				var a = armaments.FirstOrDefault(x => !x.OutOfAmmo);
				if (a == null)
					a = armaments.First();

				cursor = !target.IsInRange(self.CenterPosition, a.MaxRange())
					? ab.Info.OutsideRangeCursor ?? a.Info.OutsideRangeCursor
					: ab.Info.Cursor ?? a.Info.Cursor;

				OrderID = ab.forceAttackOrderName;
				return true;
			}

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				switch (target.Type)
				{
					case TargetType.Actor:
					case TargetType.FrozenActor:
						return CanTargetActor(self, target, ref modifiers, ref cursor);
					case TargetType.Terrain:
						return CanTargetLocation(self, self.World.Map.CellContaining(target.CenterPosition), othersAtTarget, modifiers, ref cursor);
					default:
						return false;
				}
			}

			public bool IsQueued { get; protected set; }
		}
	}
}
