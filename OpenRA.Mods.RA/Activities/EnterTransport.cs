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
using OpenRA.Mods.RA.Move;
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

			var facing = self.TraitOrDefault<IFacing>();
			if (facing != null)
			{
				var desiredFacing = Util.GetFacing(transport.CenterPosition - self.CenterPosition, 0);
				if (facing.Facing != desiredFacing)
					return Util.SequenceActivities(new Turn(desiredFacing), this);
			}

			var mobile = self.Trait<Mobile>();

			return Util.SequenceActivities(
				mobile.VisualMove(transport.Location),
				new CallFunc(() =>
				{
					cargo.Load(transport, self);
					self.World.AddFrameEndTask(w => w.Remove(self));
				}));
		}
	}
}
