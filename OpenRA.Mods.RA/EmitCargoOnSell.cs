#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA
{
	class EmitCargoOnSellInfo : ITraitInfo//, Requires<Cargo> // TODO: this breaks for no apparent reason
	{
		public object Create(ActorInitializer init) { return new EmitCargoOnSell(init); }
	}

	class EmitCargoOnSell : INotifySold
	{
		readonly Cargo cargo;
		Actor passenger;

		public EmitCargoOnSell(ActorInitializer init)
		{
			cargo = init.self.Trait<Cargo>();
		}

		public void Selling(Actor self)
		{
			// TODO: support more than one passenger
			passenger = cargo.Unload(self);
		}

		public void Sold(Actor self)
		{
			if (passenger == null)
				return;

			self.World.AddFrameEndTask(w => w.CreateActor(passenger.Info.Name, new TypeDictionary
			{
				new LocationInit(self.Location),
				new OwnerInit(self.Owner),
			}));
		}
	}
}
