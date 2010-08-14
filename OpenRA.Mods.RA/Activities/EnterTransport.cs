#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class EnterTransport : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isCanceled;
		public Actor transport;

		public EnterTransport(Actor self, Actor transport)
		{
			this.transport = transport;
		}

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			if (transport == null || !transport.IsInWorld) return NextActivity;

			var cargo = transport.Trait<Cargo>();
			if (cargo.IsFull(transport)) 
				return NextActivity;
			
			
			// Todo: Queue a move order to the transport? need to be
			// careful about units that can't path to the transport
			if ((transport.Location - self.Location).Length > 1)
				return NextActivity;
			
			cargo.Load(transport, self);
			self.World.AddFrameEndTask(w => w.Remove(self));

			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
