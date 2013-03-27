#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class EnterTransport : Activity
	{
		public Actor transport;

		public EnterTransport(Actor self, Actor transport)
		{
			this.transport = transport;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (transport == null || !transport.IsInWorld) return NextActivity;

			var cargo = transport.Trait<Cargo>();
			if (!cargo.CanLoad(transport, self))
				return NextActivity;

			// TODO: Queue a move order to the transport? need to be
			// careful about units that can't path to the transport
			if ((transport.Location - self.Location).LengthSquared > 2)
				return NextActivity;

			cargo.Load(transport, self);
			self.World.AddFrameEndTask(w => w.Remove(self));

			return this;
		}
	}
}
