#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class EnterSharedTransport : Enter
	{
		readonly SharedPassenger passenger;
		readonly int maxTries;
		Actor transport;
		SharedCargo cargo;

		public EnterSharedTransport(Actor self, Actor transport, int maxTries = 0, bool repathWhileMoving = true)
			: base(self, transport, EnterBehaviour.Exit, maxTries, repathWhileMoving)
		{
			this.transport = transport;
			this.maxTries = maxTries;
			cargo = transport.Trait<SharedCargo>();
			passenger = self.Trait<SharedPassenger>();
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
	}
}
