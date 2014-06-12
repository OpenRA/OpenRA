#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("The actor will automatically engage the enemy when it is in range.")]
	public class AutoTargetInfo : ITraitInfo, Requires<AttackBaseInfo>
	{
		[Desc("It will try to hunt down the enemy if it is not set to defend.")]
		public readonly bool AllowMovement = true;
		[Desc("Set to a value >1 to override weapons maximum range for this.")]
		public readonly int ScanRadius = -1;
		public readonly UnitStance InitialStance = UnitStance.AttackAnything;

		[Desc("Ticks to wait until next AutoTarget: attempt.")]
		public readonly int MinimumScanTimeInterval = 3;
		[Desc("Ticks to wait until next AutoTarget: attempt.")]
		public readonly int MaximumScanTimeInterval = 8;

		public readonly bool TargetWhenIdle = true;
		public readonly bool TargetWhenDamaged = true;
		public readonly bool EnableStances = true;

		public object Create(ActorInitializer init) { return new AutoTarget(init.self, this); }
	}

	public enum UnitStance { HoldFire, ReturnFire, Defend, AttackAnything }

	public class AutoTarget : INotifyIdle, INotifyDamage, ITick, IResolveOrder, ISync
	{
		readonly AutoTargetInfo info;
		readonly AttackBase attack;
		readonly AttackFollow at;
		[Sync] int nextScanTime = 0;

		public UnitStance Stance;
		[Sync] public Actor Aggressor;
		[Sync] public Actor TargetedActor;

		// NOT SYNCED: do not refer to this anywhere other than UI code
		public UnitStance PredictedStance;

		public AutoTarget(Actor self, AutoTargetInfo info)
		{
			this.info = info;
			attack = self.Trait<AttackBase>();
			Stance = info.InitialStance;
			PredictedStance = Stance;
			at = self.TraitOrDefault<AttackFollow>();
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SetUnitStance" && info.EnableStances)
				Stance = (UnitStance)order.TargetLocation.X;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!self.IsIdle || !info.TargetWhenDamaged)
				return;

			var attacker = e.Attacker;
			if (attacker.Destroyed || Stance < UnitStance.ReturnFire)
				return;

			if (!attacker.IsInWorld && !attacker.Destroyed)
			{
				// If the aggressor is in a transport, then attack the transport instead
				var passenger = attacker.TraitOrDefault<Passenger>();
				if (passenger != null && passenger.Transport != null)
					attacker = passenger.Transport;
			}

			// not a lot we can do about things we can't hurt... although maybe we should automatically run away?
			if (!attack.HasAnyValidWeapons(Target.FromActor(attacker)))
				return;

			// don't retaliate against own units force-firing on us. It's usually not what the player wanted.
			if (attacker.AppearsFriendlyTo(self))
				return;

			// don't retaliate against healers
			if (e.Damage < 0)
				return;

			Aggressor = attacker;
			if (at == null || !at.IsReachableTarget(at.Target, info.AllowMovement && Stance != UnitStance.Defend))
				Attack(self, Aggressor);
		}

		public void TickIdle(Actor self)
		{
			if (Stance < UnitStance.Defend || !info.TargetWhenIdle)
				return;

			var allowMovement = info.AllowMovement && Stance != UnitStance.Defend;
			if (at == null || !at.IsReachableTarget(at.Target, allowMovement))
				ScanAndAttack(self);
		}

		public void Tick(Actor self)
		{
			if (nextScanTime > 0)
				--nextScanTime;
		}

		public Actor ScanForTarget(Actor self, Actor currentTarget)
		{
			if (nextScanTime <= 0)
			{
				var range = info.ScanRadius > 0 ? WRange.FromCells(info.ScanRadius) : attack.GetMaximumRange();
				if (self.IsIdle || currentTarget == null || !Target.FromActor(currentTarget).IsInRange(self.CenterPosition, range))
					return ChooseTarget(self, range);
			}

			return currentTarget;
		}

		public void ScanAndAttack(Actor self)
		{
			var targetActor = ScanForTarget(self, null);
			if (targetActor != null)
				Attack(self, targetActor);
		}

		void Attack(Actor self, Actor targetActor)
		{
			TargetedActor = targetActor;
			var target = Target.FromActor(targetActor);
			self.SetTargetLine(target, Color.Red, false);
			attack.AttackTarget(target, false, info.AllowMovement && Stance != UnitStance.Defend);
		}

		Actor ChooseTarget(Actor self, WRange range)
		{
			nextScanTime = self.World.SharedRandom.Next(info.MinimumScanTimeInterval, info.MaximumScanTimeInterval);
			var inRange = self.World.FindActorsInCircle(self.CenterPosition, range);

			return inRange
				.Where(a =>
					a.AppearsHostileTo(self) &&
					!a.HasTrait<AutoTargetIgnore>() &&
					attack.HasAnyValidWeapons(Target.FromActor(a)) &&
					self.Owner.Shroud.IsTargetable(a))
				.ClosestTo(self);
		}
	}

	[Desc("Will not get automatically targeted by enemy (like walls)")]
	class AutoTargetIgnoreInfo : TraitInfo<AutoTargetIgnore> { }
	class AutoTargetIgnore { }
}
