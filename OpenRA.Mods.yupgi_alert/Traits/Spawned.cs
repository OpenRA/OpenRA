#region Copyright & License Information
/*
 * Modded by Boolbada of OP Mod.
 * Modded from cargo.cs but a lot changed.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Yupgi_alert.Activities;
using OpenRA.Traits;

/*
Works without base engine modification.
However, Mods.Common\Activities\Air\Land.cs is modified to support the air units to land "mid air!"
See landHeight private variable to track the changes.
*/

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("Can be slaved to a spawner.")]
	class SpawnedInfo : ITraitInfo
	{
		public readonly string EnterCursor = "enter";

		[Desc("Move this close to the spawner, before entering it.")]
		public readonly WDist LandingDistance = new WDist(5 * 1024);

		[Desc("We consider this is close enought to the spawner and enter it, instead of trying to reach 0 distance." +
			"This allows the spawned unit to enter the spawner while the spawner is moving.")]
		public readonly WDist CloseEnoughDistance = new WDist(128);

		public object Create(ActorInitializer init) { return new Spawned(init, this); }
	}

	class Spawned : IIssueOrder, IResolveOrder, INotifyKilled, INotifyBecomingIdle
	{
		readonly SpawnedInfo info;
		readonly AmmoPool[] ammoPools;

		Actor master = null;
		bool independent = false; // can be ordered like an independent unit?

		public Actor Master { get { return master; } }

		public Spawned(ActorInitializer init, SpawnedInfo info)
		{
			this.info = info;
			ammoPools = init.Self.TraitsImplementing<AmmoPool>().ToArray();
		}

		public void LinkMaster(Actor master, bool independent)
		{
			this.master = master;
			this.independent = independent;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new SpawnedReturnOrderTargeter(Info); }
		}

		internal SpawnedInfo Info
		{
			get
			{
				return info;
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			// If killed, I tell my master that I'm gone.
			// Can happen, when built from build palette (w00t)
			if (master == null || master.Disposed || master.IsDead)
				return;
			var spawner = master.Trait<Spawner>();
			spawner.SlaveKilled(master, self);
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			//// don't mind too much about this part.
			//// Everything is (or, should be.) automatically ordered properly by the master.

			if (order.OrderID != "SpawnedReturn")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return null;

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		static bool IsValidOrder(Actor self, Order order)
		{
			// Not targeting a frozen actor
			if (order.ExtraData == 0 && order.TargetActor == null)
				return false;

			var spawned = self.Trait<Spawned>();
			return order.TargetActor == spawned.master;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "SpawnedReturn" || !IsValidOrder(self, order))
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Green);
			if (target.Type != TargetType.Actor)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Green);
			EnterSpawner(self);
		}

		public void EnterSpawner(Actor self)
		{
			if (master == null || master.IsDead)
				self.Kill(self); // No master == death.
			else if (!(self.CurrentActivity is EnterSpawner))
			{
				var tgt = Target.FromActor(master);
				self.CancelActivity();
				if (self.TraitOrDefault<AttackPlane>() != null) // Let attack planes approach me first, before landing.
					self.QueueActivity(new Fly(self, tgt, WDist.Zero, Info.LandingDistance));
				self.QueueActivity(new EnterSpawner(self, master, EnterBehaviour.Exit, info.CloseEnoughDistance));
			}
		}

		bool NeedToReload(Actor self)
		{
			// The unit may not have ammo but will have unlimited ammunitions.
			if (ammoPools.Length == 0)
				return false;
			return ammoPools.All(x => !x.Info.SelfReloads && !x.HasAmmo());
		}

		Actor lastTarget = null;
		public virtual void AttackTarget(Actor self, Target target)
		{
			// If missile, no need for complex stuff.
			if (self.TraitOrDefault<ShootableBallisticMissile>() != null)
			{
				// SBMs may not change target
				if (self.CurrentActivity == null)
					self.QueueActivity(new ShootableBallisticMissileFly(self, self.Trait<ShootableBallisticMissile>().Target));
				return;
			}

			// Don't have to change target or alter current activity.
			if (lastTarget != null && lastTarget == target.Actor)
				return;

			// The following checks come after cancel activity because
			// these spawned units return to spawner when idle.

			// To prevent AI controlled spawned from doing stupid thing, don't do anything when no ammo,
			// so that spawned will continue to return.
			if (NeedToReload(self))
			{
				self.CancelActivity();
				return;
			}

			if (!target.IsValidFor(self))
			{
				self.CancelActivity();
				return;
			}

			// Make the spawned actor attack my target.
			if (self.TraitOrDefault<AttackPlane>() != null)
			{
				self.QueueActivity(false, new SpawnedFlyAttack(self, target)); // Different from regular attacks so not using attack base.
			}
			else if (self.TraitOrDefault<AttackHeli>() != null)
			{
				Game.Debug("Warning: AttackHeli's are not ready for spawned slave.");
				self.QueueActivity(false, new HeliAttack(self, target)); // not ready for helis...
			}
			else
			{
				foreach (var atb in self.TraitsImplementing<AttackBase>())
				{
					if (target.Actor == null)
						atb.AttackTarget(target, false, true, true); // force fire on the ground.
					else if (target.Actor.Owner.Stances[self.Owner] == Stance.Ally)
						atb.AttackTarget(target, false, true, true); // force fire on ally.
					else
						/* Target deprives me of force fire information.
						 * This is a glitch if force fire weapon and normal fire are different, as in
						 * RA mod spies but won't matter too much for carriers.
						 */
						atb.AttackTarget(target, false, true, target.RequiresForceFire);
				}
			}	
		}

		public virtual void OnBecomingIdle(Actor self)
		{
			if (independent)
				return;

			// Return when nothing to attack.
			// Don't let myself to circle around the player's construction yard.
			if (self.TraitOrDefault<ShootableBallisticMissile>() != null)
				self.QueueActivity(false, new CallFunc(() => self.Kill(self)));
			else
				EnterSpawner(self);
		}

		class SpawnedReturnOrderTargeter : UnitOrderTargeter
		{
			public SpawnedReturnOrderTargeter(SpawnedInfo info)
				: base("SpawnedReturn", 6, info.EnterCursor, false, true)
			{
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				if (!target.Info.HasTraitInfo<SpawnerInfo>())
					return false;

				// can only enter player owned one.
				if (self.Owner != target.Owner)
					return false;

				var spawned = self.Trait<Spawned>();

				if (target != spawned.master)
					return false;

				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				// You can't enter frozen actor.
				return false;
			}
		}
	}
}
