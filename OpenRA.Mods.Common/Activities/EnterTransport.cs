#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class EnterTransport : Enter
	{
		readonly Passenger passenger;

		Actor enterActor;
		Cargo enterCargo;

		public EnterTransport(Actor self, Target target)
			: base(self, target, Color.Green)
		{
			passenger = self.Trait<Passenger>();
		}

		protected override bool TryStartEnter(Actor self, Actor targetActor)
		{
			enterActor = targetActor;
			enterCargo = targetActor.TraitOrDefault<Cargo>();

			// Make sure we can still enter the transport
			// (but not before, because this may stop the actor in the middle of nowhere)
			if (enterCargo == null || !passenger.Reserve(self, enterCargo))
			{
				Cancel(self, true);
				return false;
			}

			return true;
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			self.World.AddFrameEndTask(w =>
			{
				// Make sure the target hasn't changed while entering
				// OnEnterComplete is only called if targetActor is alive
				if (targetActor != enterActor)
					return;

				if (!enterCargo.CanLoad(enterActor, self))
					return;

				enterCargo.Load(enterActor, self);
				w.Remove(self);

				// Preemptively cancel any activities to avoid an edge-case where successively queued
				// EnterTransports corrupt the actor state. Activities are cancelled again on unload
				self.CancelActivity();
			});
		}

		protected override void OnCancel(Actor self)
		{
			passenger.Unreserve(self);
		}

		protected override void OnLastRun(Actor self)
		{
			passenger.Unreserve(self);
		}
	}

	class EnterTransports : Activity
	{
		readonly string type;
		readonly Passenger passenger;

		Activity enterTransport;

		public EnterTransports(Actor self, Target primaryTarget)
		{
			passenger = self.Trait<Passenger>();
			if (primaryTarget.Type == TargetType.Actor)
				type = primaryTarget.Actor.Info.Name;

			enterTransport = new EnterTransport(self, primaryTarget);
		}

		public override Activity Tick(Actor self)
		{
			if (enterTransport != null)
			{
				enterTransport = ActivityUtils.RunActivity(self, enterTransport);
				if (enterTransport != null)
					return this;
			}

			// Try and find a new transport nearby
			if (IsCanceled || string.IsNullOrEmpty(type))
				return NextActivity;

			Func<Actor, bool> isValidTransport = a =>
			{
				var c = a.TraitOrDefault<Cargo>();
				return c != null && c.Info.Types.Contains(passenger.Info.CargoType) &&
				       (c.Unloading || c.CanLoad(a, self));
			};

			var candidates = self.World.FindActorsInCircle(self.CenterPosition, passenger.Info.AlternateTransportScanRange)
				.Where(isValidTransport)
				.ToList();

			// Prefer transports of the same type as the primary
			var transport = candidates.Where(a => a.Info.Name == type).ClosestTo(self);
			if (transport == null)
				transport = candidates.ClosestTo(self);

			if (transport != null)
			{
				enterTransport = ActivityUtils.RunActivity(self, new EnterTransport(self, Target.FromActor(transport)));
				return this;
			}

			return NextActivity;
		}

		public override bool Cancel(Actor self, bool keepQueue = false)
		{
			if (!IsCanceled && enterTransport != null && !enterTransport.Cancel(self))
				return false;

			return base.Cancel(self, keepQueue);
		}
	}
}
