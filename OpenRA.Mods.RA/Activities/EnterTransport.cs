#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class EnterTransport : Activity
	{
		readonly Actor transport;
		readonly Cargo cargo;

		public EnterTransport(Actor self, Actor transport)
		{
			this.transport = transport;
			cargo = transport.Trait<Cargo>();
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			if (transport == null || !transport.IsInWorld)
				return NextActivity;

			if (!cargo.CanLoad(transport, self))
				return NextActivity;

			// TODO: Queue a move order to the transport? need to be
			// careful about units that can't path to the transport
			var cells = Util.AdjacentCells(Target.FromActor(transport));
			if (!cells.Contains(self.Location))
				return NextActivity;

			self.World.AddFrameEndTask(w => 
			{
				if(self.IsDead() || transport.IsDead() || !cargo.CanLoad(transport, self))
					return;

				cargo.Load(transport, self);
				w.Remove(self);
			});

			return this;
		}
	}
}
