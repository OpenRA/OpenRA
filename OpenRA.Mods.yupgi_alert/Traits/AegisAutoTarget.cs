#region Copyright & License Information
/*
 * CnP of AutoTarget.cs
 * Could have used inheritance but to allow users to use this module
 * without base engine modification...
 * Modded by Boolbada of OP Mod
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

/* Works without base engine modification, although you'd want facing tolerance fix */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("The actor will fire shots equally to many targets nearby, unless told to focus fire.")]
	public class AegisAutoTargetInfo : AutoTargetInfo, Requires<AttackFrontalInfo>, UsesInit<StanceInit>
	{
		public override object Create(ActorInitializer init) { return new AegisAutoTarget(init, this); }
	}

	public class AegisAutoTarget : ConditionalTrait<AegisAutoTargetInfo>, INotifyIdle,
		INotifyDamage, ITick, IResolveOrder, ISync, INotifyAttack
	{
		readonly IEnumerable<AttackBase> activeAttackBases;
		readonly AttackFollow[] attackFollows;

		readonly AegisAutoTargetInfo info;
		readonly AttackFrontalInfo afi;
		readonly Lazy<IFacing> facing;

		[Sync]
		int nextScanTime = 0;

		public UnitStance Stance;
		[Sync]
		public Actor Aggressor;
		[Sync]
		public Actor TargetedActor;

		// NOT SYNCED: do not refer to this anywhere other than UI code
		public UnitStance PredictedStance;

		bool rrenabled = true;
		int targetIndex;
		List<Actor> cachedTargets = new List<Actor>();

		public AegisAutoTarget(ActorInitializer init, AegisAutoTargetInfo info)
			: base(info)
		{
			var self = init.Self;
			activeAttackBases = self.TraitsImplementing<AttackBase>().ToArray().Where(Exts.IsTraitEnabled);

			if (init.Contains<StanceInit>())
				Stance = init.Get<StanceInit, UnitStance>();
			else
				Stance = self.Owner.IsBot || !self.Owner.Playable ? info.InitialStanceAI : info.InitialStance;

			PredictedStance = Stance;
			attackFollows = self.TraitsImplementing<AttackFollow>().ToArray();

			this.info = info;
			facing = Exts.Lazy(() => init.Self.TraitOrDefault<IFacing>());
			afi = init.Self.Info.TraitInfo<AttackFrontalInfo>();
		}

		public void ResolveOrder(Actor self, Order order)
		{
			rrenabled = true;
			if (order.OrderString == "SetUnitStance" && info.EnableStances)
				Stance = (UnitStance)order.ExtraData;
			else if (order.OrderString == "Attack" && info.EnableStances)
				rrenabled = false;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (!self.IsIdle || !Info.TargetWhenDamaged)
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
			var attackerAsTarget = Target.FromActor(attacker);
			if (!activeAttackBases.Any(a => a.HasAnyValidWeapons(attackerAsTarget)))
				return;

			// don't retaliate against own units force-firing on us. It's usually not what the player wanted.
			if (attacker.AppearsFriendlyTo(self))
				return;

			// don't retaliate against healers
			if (e.Damage.Value < 0)
				return;

			Aggressor = attacker;

			bool allowMove;
			if (ShouldAttack(out allowMove))
				Attack(self, Aggressor, allowMove);
		}

		public void TickIdle(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (Stance < UnitStance.Defend || !Info.TargetWhenIdle)
				return;

			bool allowMove;
			if (ShouldAttack(out allowMove))
				ScanAndAttack(self, allowMove);
		}

		bool ShouldAttack(out bool allowMove)
		{
			allowMove = Info.AllowMovement && Stance != UnitStance.Defend;

			// PERF: Avoid LINQ.
			foreach (var attackFollow in attackFollows)
				if (!attackFollow.IsTraitDisabled && attackFollow.IsReachableTarget(attackFollow.Target, allowMove))
					return false;

			return true;
		}

		public void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (nextScanTime > 0)
				--nextScanTime;
		}

		// CnP but added cachedTargets and give random targetIndex + RoundRobin.
		public Actor ScanForTarget(Actor self, bool allowMove)
		{
			if (nextScanTime <= 0 && activeAttackBases.Any())
			{
				nextScanTime = self.World.SharedRandom.Next(Info.MinimumScanTimeInterval, Info.MaximumScanTimeInterval);

				foreach (var ab in activeAttackBases)
				{
					// If we can't attack right now, there's no need to try and find a target.
					var attackStances = ab.UnforcedAttackTargetStances();
					if (attackStances != OpenRA.Traits.Stance.None)
					{
						var range = Info.ScanRadius > 0 ? WDist.FromCells(Info.ScanRadius) : ab.GetMaximumRange();
						cachedTargets = ChooseTargets(self, ab, attackStances, range, allowMove);
						if (cachedTargets != null)
						{
							targetIndex = targetIndex % cachedTargets.Count();
							break;
						}
					}
				}
			}

			return RoundRobin(self, true);
		}	

		public void ScanAndAttack(Actor self, bool allowMove)
		{
			var targetActor = ScanForTarget(self, allowMove);
			if (targetActor != null)
				Attack(self, targetActor, allowMove);
		}

		void Attack(Actor self, Actor targetActor, bool allowMove)
		{
			TargetedActor = targetActor;
			var target = Target.FromActor(targetActor);
			self.SetTargetLine(target, Color.Red, false);

			foreach (var ab in activeAttackBases)
				ab.AttackTarget(target, false, allowMove);
		}

		// CnP from ChooseTargets but doesn't choose one and return all.
		List<Actor> ChooseTargets(Actor self, AttackBase ab, Stance attackStances, WDist range, bool allowMove)
		{
			var actorsByArmament = new Dictionary<Armament, List<Actor>>();
			var actorsInRange = self.World.FindActorsInCircle(self.CenterPosition, range);
			foreach (var actor in actorsInRange)
			{
				// PERF: Most units can only attack enemy units. If this is the case but the target is not an enemy, we
				// can bail early and avoid the more expensive targeting checks and armament selection. For groups of
				// allied units, this helps significantly reduce the cost of auto target scans. This is important as
				// these groups will continuously rescan their allies until an enemy finally comes into range.
				if (attackStances == OpenRA.Traits.Stance.Enemy && !actor.AppearsHostileTo(self))
					continue;

				if (PreventsAutoTarget(self, actor) || !self.Owner.CanTargetActor(actor))
					continue;

				// Select only the first compatible armament for each actor: if this actor is selected
				// it will be thanks to the first armament anyways, since that is the first selection
				// criterion
				var target = Target.FromActor(actor);
				var armaments = ab.ChooseArmamentsForTarget(target, false);
				if (!allowMove)
					armaments = armaments.Where(arm =>
						target.IsInRange(self.CenterPosition, arm.MaxRange()) &&
						!target.IsInRange(self.CenterPosition, arm.Weapon.MinRange));

				var armament = armaments.FirstOrDefault();
				if (armament == null)
					continue;

				List<Actor> actors;
				if (actorsByArmament.TryGetValue(armament, out actors))
					actors.Add(actor);
				else
					actorsByArmament.Add(armament, new List<Actor> { actor });
			}

			// Armaments are enumerated in attack. Armaments in construct order.
			// When autotargeting, first choose targets according to the used armament construct order
			// And then according to distance from actor
			// This enables preferential treatment of certain armaments
			// (e.g. tesla trooper's tesla zap should have precedence over tesla charge)
			foreach (var arm in ab.Armaments)
			{
				List<Actor> actors;
				if (actorsByArmament.TryGetValue(arm, out actors))
					return actors;
			}

			return null;
		}

		bool PreventsAutoTarget(Actor attacker, Actor target)
		{
			foreach (var pat in target.TraitsImplementing<IPreventsAutoTarget>())
				if (pat.PreventsAutoTarget(target, attacker))
					return true;

			return false;
		}

		bool HaveToTurn(Actor self, Actor target)
		{
			var f = facing.Value.Facing;
			var delta = target.CenterPosition - self.CenterPosition;
			var facingToTarget = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : f;

			if (Math.Abs(facingToTarget - f) % 256 > afi.FacingTolerance)
				return true;

			return false;
		}

		// Switch target after firing ONE shot.
		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (!rrenabled)
				return;

			// probably not this case
			if (cachedTargets == null || cachedTargets.Count() == 0)
				return;

			Actor tgt = RoundRobin(self, false);
			if (tgt != null)
			{
				Attack(self, tgt, false);
				return;
			}

			// We have targets but cant attack. That's because we scanned targets we don't have to turn.
			tgt = RoundRobin(self, true);
			if (tgt != null)
				Attack(self, tgt, false);
		}

		public void PreparingAttack(Actor self, Target target, Armament a, Barrel barrel)
		{
			// do nothing
		}

		Actor RoundRobin(Actor self, bool allowTurn)
		{
			// We should scan frequently enough to prevent Aegis system from becoming stupid.
			if (cachedTargets == null)
				return null;

			for (int j = 0; j < cachedTargets.Count(); j++)
			{
				Actor a = cachedTargets[(targetIndex + j) % cachedTargets.Count()];
				if (a.IsDead || a.Disposed)
					continue;

				if (!allowTurn && HaveToTurn(self, a))
					continue;

				targetIndex++; // attack something else next time.
				if (targetIndex >= cachedTargets.Count())
					targetIndex = 0;
				return a;
			}

			cachedTargets.Clear();
			return null;
		}
	}
}
