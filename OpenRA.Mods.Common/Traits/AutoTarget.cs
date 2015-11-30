#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("The actor will automatically engage the enemy when it is in range.")]
	public class AutoTargetInfo : ITraitInfo, Requires<AttackBaseInfo>, UsesInit<StanceInit>
	{
		[Desc("It will try to hunt down the enemy if it is not set to defend.")]
		public readonly bool AllowMovement = true;

		[Desc("Set to a value >1 to override weapons maximum range for this.")]
		public readonly int ScanRadius = -1;

		[Desc("Possible values are HoldFire, ReturnFire, Defend and AttackAnything.",
			"Used for computer-controlled players, both Lua-scripted and regular Skirmish AI alike.")]
		public readonly UnitStance InitialStanceAI = UnitStance.AttackAnything;

		[Desc("Possible values are HoldFire, ReturnFire, Defend and AttackAnything. Used for human players.")]
		public readonly UnitStance InitialStance = UnitStance.Defend;

		[Desc("Allow the player to change the unit stance.")]
		public readonly bool EnableStances = true;

		[Desc("Ticks to wait until next AutoTarget: attempt.")]
		public readonly int MinimumScanTimeInterval = 3;

		[Desc("Ticks to wait until next AutoTarget: attempt.")]
		public readonly int MaximumScanTimeInterval = 8;

		public readonly bool TargetWhenIdle = true;

		public readonly bool TargetWhenDamaged = true;

		public object Create(ActorInitializer init) { return new AutoTarget(init, this); }
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

		public AutoTarget(ActorInitializer init, AutoTargetInfo info)
		{
			var self = init.Self;
			this.info = info;
			attack = self.Trait<AttackBase>();

			if (init.Contains<StanceInit>())
				Stance = init.Get<StanceInit, UnitStance>();
			else
				Stance = self.Owner.IsBot || !self.Owner.Playable ? info.InitialStanceAI : info.InitialStance;

			PredictedStance = Stance;
			at = self.TraitOrDefault<AttackFollow>();
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SetUnitStance" && info.EnableStances)
				Stance = (UnitStance)order.ExtraData;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!self.IsIdle || !info.TargetWhenDamaged)
				return;

			var attacker = e.Attacker;
			if (attacker.Disposed || Stance < UnitStance.ReturnFire)
				return;

			if (!attacker.IsInWorld && !attacker.Disposed)
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
			var allowMove = info.AllowMovement && Stance != UnitStance.Defend;
			if (at == null || !at.IsReachableTarget(at.Target, allowMove))
				Attack(self, Aggressor, allowMove);
		}

		public void TickIdle(Actor self)
		{
			if (Stance < UnitStance.Defend || !info.TargetWhenIdle)
				return;

			var allowMove = info.AllowMovement && Stance != UnitStance.Defend;
			if (at == null || !at.IsReachableTarget(at.Target, allowMove))
				ScanAndAttack(self, allowMove);
		}

		public void Tick(Actor self)
		{
			if (nextScanTime > 0)
				--nextScanTime;
		}

		public Actor ScanForTarget(Actor self, Actor currentTarget, bool allowMove)
		{
			if (nextScanTime <= 0)
			{
				var range = info.ScanRadius > 0 ? WDist.FromCells(info.ScanRadius) : attack.GetMaximumRange();
				if (self.IsIdle || currentTarget == null || !Target.FromActor(currentTarget).IsInRange(self.CenterPosition, range))
				{
					nextScanTime = self.World.SharedRandom.Next(info.MinimumScanTimeInterval, info.MaximumScanTimeInterval);
					return ChooseTarget(self, range, allowMove);
				}
			}

			return currentTarget;
		}

		public void ScanAndAttack(Actor self, bool allowMove)
		{
			var targetActor = ScanForTarget(self, null, allowMove);
			if (targetActor != null)
				Attack(self, targetActor, allowMove);
		}

		void Attack(Actor self, Actor targetActor, bool allowMove)
		{
			TargetedActor = targetActor;
			var target = Target.FromActor(targetActor);
			self.SetTargetLine(target, Color.Red, false);
			attack.AttackTarget(target, false, allowMove);
		}

		Actor ChooseTarget(Actor self, WDist range, bool allowMove)
		{
			var inRange = self.World.FindActorsInCircle(self.CenterPosition, range)
				.Where(a =>
					!a.TraitsImplementing<IPreventsAutoTarget>().Any(t => t.PreventsAutoTarget(a, self)));

			// Armaments are enumerated in attack.Armaments in construct order
			// When autotargeting, first choose targets according to the used armament construct order
			// And then according to distance from actor
			// This enables preferential treatment of certain armaments
			// (e.g. tesla trooper's tesla zap should have precedence over tesla charge)
			var actorByArmament = inRange

				// Select only the first compatible armament for each actor: if this actor is selected
				// it will be thanks to the first armament anyways, since that is the first selection
				// criterion
				.Select(a =>
				{
					var target = Target.FromActor(a);
					return new KeyValuePair<Armament, Actor>(
						attack.ChooseArmamentsForTarget(target, false)
							.FirstOrDefault(arm => allowMove
								|| (target.IsInRange(self.CenterPosition, arm.MaxRange())
								&& !target.IsInRange(self.CenterPosition, arm.Weapon.MinRange))), a);
				})

				.Where(kv => kv.Key != null && self.Owner.CanTargetActor(kv.Value))
				.GroupBy(kv => kv.Key, kv => kv.Value)
				.ToDictionary(kv => kv.Key, kv => kv.ClosestTo(self));

			foreach (var arm in attack.Armaments)
			{
				Actor actor;
				if (actorByArmament.TryGetValue(arm, out actor))
					return actor;
			}

			return null;
		}
	}

	[Desc("Will not get automatically targeted by enemy (like walls)")]
	class AutoTargetIgnoreInfo : TraitInfo<AutoTargetIgnore> { }
	class AutoTargetIgnore : IPreventsAutoTarget
	{
		public bool PreventsAutoTarget(Actor self, Actor attacker)
		{
			return true;
		}
	}

	public class StanceInit : IActorInit<UnitStance>
	{
		[FieldFromYamlKey] readonly UnitStance value = UnitStance.AttackAnything;
		public StanceInit() { }
		public StanceInit(UnitStance init) { value = init; }
		public UnitStance Value(World world) { return value; }
	}
}
