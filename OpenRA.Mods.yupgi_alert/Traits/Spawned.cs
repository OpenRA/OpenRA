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

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.yupgi_alert.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.yupgi_alert.Traits
{
	[Desc("Can be slaved to a spawner.")]
	class SpawnedInfo : ITraitInfo
	{
		public readonly string EnterCursor = "enter";

		[Desc("Move this close to the spawner, before entering it.")]
		public readonly WDist LandingDistance = new WDist(5*1024);

		public object Create(ActorInitializer init) { return new Spawned(init, this); }
	}

	class Spawned : IIssueOrder, IResolveOrder, INotifyKilled, INotifyBecomingIdle
	{
		readonly SpawnedInfo info;
		public Actor Master = null;

		public Spawned(ActorInitializer init, SpawnedInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new SpawnedReturnOrderTargeter(info); }
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			// If killed, I tell my master that I'm gone.
			if (Master == null)
				// Can happen, when built from build palette (w00t)
				return;
			var spawner = Master.Trait<Spawner>();
			spawner.SlaveKilled(Master, self);
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			// don't mind too much about this part.
			// Everything is (or, should be.) automatically ordered properly by the master.

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
			return order.TargetActor == spawned.Master;
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
			if (Master == null)
				self.Kill(self); // No master == death.
			else
			{
				var tgt = Target.FromActor(Master);
				self.CancelActivity();
				if (self.TraitOrDefault<AttackPlane>() != null) // Let attack planes approach me first, before landing.
					self.QueueActivity(new Fly(self, tgt, WDist.Zero, info.LandingDistance));
				self.QueueActivity(new EnterSpawner(self, Master, EnterBehaviour.Exit));
			}
		}

		Actor last_target = null;
		public virtual void AttackTarget(Actor self, Target target)
		{
			if (last_target != null && last_target == target.Actor)
				// Don't have to change target or alter current activity.
				return;

			self.CancelActivity();

			// Make the spawned actor attack my target.
			if (self.TraitOrDefault<AttackPlane>() != null)
			{
				self.QueueActivity(new SpawnedFlyAttack(self, target)); // Different from regular attacks so not using attack base.
			}
			else if (self.TraitOrDefault<AttackHeli>() != null)
			{
				Game.Debug("Warning: AttackHeli's are not ready for spawned slave.");
				self.QueueActivity(new HeliAttack(self, target)); // not ready for helis...
			}
			else
			{
				foreach (var atb in self.TraitsImplementing<AttackBase>())
				{
					if (target.Actor == null)
						atb.AttackTarget(target, true, true, true); // force fire on the ground.
					else if (target.Actor.Owner.Stances[self.Owner] == Stance.Ally)
						atb.AttackTarget(target, true, true, true); // force fire on ally.
					else
						// Well, target deprives me of force fire information.
						// This is a glitch if force fire weapon and normal fire are different, as in
						// RA mod spies but won't matter too much for carriers.
						atb.AttackTarget(target, true, true, target.RequiresForceFire);
				}
			}	
		}

		public virtual void OnBecomingIdle(Actor self)
		{
			// Return when nothing to attack.
			// Don't let myself to circle around the player's construction yard.
			EnterSpawner(self);
		}

		class SpawnedReturnOrderTargeter : UnitOrderTargeter
		{
			SpawnedInfo info;

			public SpawnedReturnOrderTargeter(SpawnedInfo info)
				: base("SpawnedReturn", 6, info.EnterCursor, false, true)
			{
				this.info = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				if (!target.Info.HasTraitInfo<SpawnerInfo>())
					return false;

				if (self.Owner != target.Owner)
					// can only enter player owned one.
					return false;

				var spawned = self.Trait<Spawned>();

				if (target != spawned.Master)
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