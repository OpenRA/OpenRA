#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k
{
	[Desc("Automatically transports harvesters with the Carryable trait between resource fields and refineries")]
	public class AutoCarryallInfo : ITraitInfo, Requires<IBodyOrientationInfo>
	{
		public object Create(ActorInitializer init) { return new AutoCarryall(init.self, this); }
	}

	public class AutoCarryall : INotifyBecomingIdle, INotifyKilled, ISync, IRender
	{
		readonly Actor self;
		readonly WRange carryHeight;

		// The actor we are currently carrying.
		[Sync] Actor carrying;

		// TODO: Use ActorPreviews so that this can support actors with multiple sprites
		Animation anim;

		public bool Busy { get; internal set; }

		public AutoCarryall(Actor self, AutoCarryallInfo info)
		{
			this.self = self;
			carryHeight = self.Trait<Helicopter>().Info.LandAltitude;
		}

		public void OnBecomingIdle(Actor self)
		{
			FindCarryableForTransport();

			if (!Busy)
				self.QueueActivity(new HeliFlyCircle(self));
		}

		// A carryable notifying us that he'd like to be carried
		public bool RequestTransportNotify(Actor carryable)
		{
			if (Busy)
				return false;

			if (ReserveCarryable(carryable))
			{
				self.QueueActivity(false, new CarryUnit(self, carryable));
				return true;
			}

			return false;
		}

		void FindCarryableForTransport()
		{
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
					self.QueueActivity(false, new CarryUnit(self, p.Actor));
					break;
				}
			}
		}

		// Reserve the carryable so its ours exclusively
		public bool ReserveCarryable(Actor carryable)
		{
			if (carryable.Trait<Carryable>().Reserve(self))
			{
				carrying = carryable;
				Busy = true;
				return true;
			}

			return false;
		}

		// Unreserve the carryable
		public void UnreserveCarryable()
		{
			if (carrying != null)
			{
				if (carrying.IsInWorld && !carrying.IsDead)
					carrying.Trait<Carryable>().UnReserve(self);

				carrying = null;
			}

			Busy = false;
		}

		// INotifyKilled
		public void Killed(Actor self, AttackInfo e)
		{
			if (carrying != null)
			{
				carrying.Kill(e.Attacker);
				carrying = null;
			}

			UnreserveCarryable();
		}

		// Called when carryable is inside.
		public void AttachCarryable(Actor carryable)
		{
			// Create a new animation for our carryable unit
			anim = new Animation(self.World, RenderSprites.GetImage(carryable.Info), RenderSprites.MakeFacingFunc(self));
			anim.PlayRepeating("idle");
			anim.IsDecoration = true;
		}

		// Called when released
		public void CarryableReleased()
		{
			anim = null;
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			// Render the carryable below us TODO: Implement RenderSprites trait
			if (anim != null && !self.World.FogObscures(self))
			{
				anim.Tick();
				var renderables = anim.Render(self.CenterPosition + new WVec(0, 0, -carryHeight.Range), wr.Palette("player" + carrying.Owner.InternalName));

				foreach (var rr in renderables)
					yield return rr;
			}
		}
	}
}
