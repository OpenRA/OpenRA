#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.D2k.Activities;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Automatically transports harvesters with the Carryable trait between resource fields and refineries")]
	public class AutoCarryallInfo : ITraitInfo, Requires<IBodyOrientationInfo>
	{
		[Desc("Set to false when the carryall should not automatically get new jobs")]
		public readonly bool Automatic = true;

		public object Create(ActorInitializer init) { return new AutoCarryall(init.Self, this); }
	}

	public class AutoCarryall : INotifyBecomingIdle, INotifyKilled, ISync, IRender
	{
		readonly Actor self;
		readonly WRange carryHeight;
		readonly AutoCarryallInfo info;

		// The actor we are currently carrying.
		[Sync] public Actor Carrying { get; internal set; }

		public bool HasCarryableAttached { get; internal set; }

		// TODO: Use ActorPreviews so that this can support actors with multiple sprites
		Animation anim;

		public bool Busy { get; internal set; }

		public AutoCarryall(Actor self, AutoCarryallInfo info)
		{
			this.self = self;
			carryHeight = self.Trait<Helicopter>().Info.LandAltitude;
			this.info = info;
			Busy = false;

			HasCarryableAttached = false;
		}

		public void OnBecomingIdle(Actor self)
		{
			if (info.Automatic)
				FindCarryableForTransport();

			if (!Busy)
				self.QueueActivity(new HeliFlyCircle(self));
		}

		// A carryable notifying us that he'd like to be carried
		public bool RequestTransportNotify(Actor carryable)
		{
			if (Busy || !info.Automatic)
				return false;

			if (ReserveCarryable(carryable))
			{
				self.QueueActivity(false, new PickupUnit(self, carryable));
				self.QueueActivity(true, new DeliverUnit(self));
				return true;
			}

			return false;
		}

		void FindCarryableForTransport()
		{
			if (!self.IsInWorld)
				return;

			// get all carryables who want transport
			var carryables = self.World.ActorsWithTrait<Carryable>()
				.Where(p =>
			{
				var actor = p.Actor;
				if (actor == null)
					return false;

				if (actor.Owner != self.Owner)
					return false;

				if (actor.IsDead)
					return false;

				var trait = p.Trait;
				if (trait.Reserved)
					return false;

				if (!trait.WantsTransport)
					return false;

				return true;
			})
				.OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			foreach (var p in carryables)
			{
				// Check if its actually me who's the best candidate
				if (p.Trait.GetClosestIdleCarrier() == self && ReserveCarryable(p.Actor))
				{
					self.QueueActivity(false, new PickupUnit(self, p.Actor));
					self.QueueActivity(true, new DeliverUnit(self));
					break;
				}
			}
		}

		// Reserve the carryable so its ours exclusively
		public bool ReserveCarryable(Actor carryable)
		{
			if (carryable.Trait<Carryable>().Reserve(self))
			{
				Carrying = carryable;
				Busy = true;
				return true;
			}

			return false;
		}

		// Unreserve the carryable
		public void UnreserveCarryable()
		{
			if (Carrying != null)
			{
				if (Carrying.IsInWorld && !Carrying.IsDead)
					Carrying.Trait<Carryable>().UnReserve(self);

				Carrying = null;
			}

			Busy = false;
		}

		// INotifyKilled
		public void Killed(Actor self, AttackInfo e)
		{
			if (Carrying != null)
			{
				Carrying.Kill(e.Attacker);
				Carrying = null;
			}

			UnreserveCarryable();
		}

		// Called when carryable is inside.
		public void AttachCarryable(Actor carryable)
		{
			HasCarryableAttached = true;
			Busy = true;
			Carrying = carryable;

			// Create a new animation for our carryable unit
			anim = new Animation(self.World, RenderSprites.GetImage(carryable.Info), RenderSprites.MakeFacingFunc(self));
			anim.PlayRepeating("idle");
			anim.IsDecoration = true;
		}

		// Called when released
		public void CarryableReleased()
		{
			HasCarryableAttached = false;
			anim = null;
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			// Render the carryable below us TODO: Implement RenderSprites trait
			if (anim != null && !self.World.FogObscures(self))
			{
				anim.Tick();
				var renderables = anim.Render(self.CenterPosition + new WVec(0, 0, -carryHeight.Range), wr.Palette("player" + Carrying.Owner.InternalName));

				foreach (var rr in renderables)
					yield return rr;
			}
		}
	}
}
