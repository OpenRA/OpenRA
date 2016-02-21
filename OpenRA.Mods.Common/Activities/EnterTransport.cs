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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class EnterTransport : Enter
	{
		readonly Actor transport;
		readonly Passenger passenger;
		readonly int maxTries;
		Cargo cargo;

		public EnterTransport(Actor self, Actor transport, int maxTries = 0, bool targetCenter = false)
			: base(self, transport, EnterBehaviour.Exit, maxTries, targetCenter)
		{
			this.transport = transport;
			this.maxTries = maxTries;
			cargo = transport.Trait<Cargo>();
			passenger = self.Trait<Passenger>();
		}

		protected override void Unreserve(Actor self, bool abort) { passenger.Unreserve(self); }
		protected override bool CanReserve(Actor self) { return cargo.Unloading || cargo.CanLoad(transport, self); }
		protected override ReserveStatus Reserve(Actor self)
		{
			var status = base.Reserve(self);
			if (status != ReserveStatus.Ready)
				return status;
			if (passenger.Reserve(self, cargo))
				return ReserveStatus.Ready;
			return ReserveStatus.Pending;
		}

		protected override void OnInside(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead || transport.IsDead || !cargo.CanLoad(transport, self))
					return;

				cargo.Load(transport, self);
				w.Remove(self);
			});

			Done(self);
		}

		protected override bool TryGetAlternateTarget(Actor self, int tries, ref Target target)
		{
			if (tries > maxTries)
				return false;
			var type = target.Actor.Info.Name;
			return TryGetAlternateTargetInCircle(
				self, passenger.Info.AlternateTransportScanRange,
				t => cargo = t.Actor.Trait<Cargo>(), // update cargo
				a => { var c = a.TraitOrDefault<Cargo>(); return c != null && (c.Unloading || c.CanLoad(a, self)); },
				new Func<Actor, bool>[] { a => a.Info.Name == type }); // Prefer transports of the same type
		}
	}
}
