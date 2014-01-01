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
		public Actor Transport;

		public EnterTransport(Actor self, Actor transport)
		{
			Transport = transport;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (Transport == null || !Transport.IsInWorld) return NextActivity;

			var cargo = Transport.Trait<Cargo>();
			if (!cargo.CanLoad(Transport, self))
				return NextActivity;

			if ((Transport.Location - self.Location).LengthSquared > 2)
				return NextActivity;

			cargo.Load(Transport, self);
			self.World.AddFrameEndTask(w => w.Remove(self));

			return this;
		}
	}
}
