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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Automatically transports harvesters with the Carryable trait between resource fields and refineries.")]
	public class CarryallInfo : ITraitInfo, Requires<BodyOrientationInfo>
	{
		[Desc("Set to false when the carryall should not automatically get new jobs.")]
		public readonly bool Automatic = true;

		public object Create(ActorInitializer init) { return new Carryall(init.Self, this); }
	}

	public class Carryall : INotifyBecomingIdle, INotifyKilled, ISync, IRender, INotifyActorDisposing
	{
		readonly Actor self;
		readonly WDist carryHeight;
		readonly CarryallInfo info;

		// The actor we are currently carrying.
		[Sync] public Actor Carrying { get; internal set; }
		public bool IsCarrying { get; internal set; }

		// TODO: Use ActorPreviews so that this can support actors with multiple sprites
		Animation anim;

		public bool IsBusy { get; internal set; }

		public Carryall(Actor self, CarryallInfo info)
		{
			this.self = self;
			this.info = info;

			IsBusy = false;
			IsCarrying = false;

			var helicopter = self.Info.TraitInfoOrDefault<AircraftInfo>();
			carryHeight = helicopter != null ? helicopter.LandAltitude : WDist.Zero;
		}

		public void OnBecomingIdle(Actor self)
		{
			if (info.Automatic)
				FindCarryableForTransport();

			if (!IsBusy)
				self.QueueActivity(new HeliFlyCircle(self));
		}

		// A carryable notifying us that he'd like to be carried
		public bool RequestTransportNotify(Actor carryable)
		{
			if (IsBusy || !info.Automatic)
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

			// Get all carryables who want transport
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

					if (actor.IsIdle)
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
			if (Carrying != null)
				return false;

			if (carryable.Trait<Carryable>().Reserve(self))
			{
				Carrying = carryable;
				IsBusy = true;
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

			CarryableReleased();
		}

		// INotifyKilled
		public void Killed(Actor self, AttackInfo e)
		{
			if (Carrying != null)
			{
				if (IsCarrying && Carrying.IsInWorld && !Carrying.IsDead)
					Carrying.Kill(e.Attacker);

				Carrying = null;
			}

			UnreserveCarryable();
		}

		public void Disposing(Actor self)
		{
			if (Carrying != null && IsCarrying)
			{
				Carrying.Dispose();
				Carrying = null;
			}
		}

		// Called when carryable is inside.
		public void AttachCarryable(Actor carryable)
		{
			IsBusy = true;
			IsCarrying = true;
			Carrying = carryable;

			// Create a new animation for our carryable unit
			var rs = carryable.Trait<RenderSprites>();
			anim = new Animation(self.World, rs.GetImage(carryable), RenderSprites.MakeFacingFunc(self));
			anim.PlayRepeating("idle");
			anim.IsDecoration = true;
		}

		// Called when released
		public void CarryableReleased()
		{
			IsBusy = false;
			IsCarrying = false;
			anim = null;
		}

		public IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			// Render the carryable below us TODO: Implement RenderSprites trait
			if (anim != null && !self.World.FogObscures(self))
			{
				anim.Tick();
				var renderables = anim.Render(self.CenterPosition + new WVec(0, 0, -carryHeight.Length),
					wr.Palette("player" + Carrying.Owner.InternalName));

				foreach (var rr in renderables)
					yield return rr;
			}
		}
	}
}
