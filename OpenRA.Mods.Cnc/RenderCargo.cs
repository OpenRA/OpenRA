#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	public class RenderCargoInfo : ITraitInfo, Requires<CargoInfo>
	{
		/* altitude of the cargo, relative to us. -ve is underneath us */
		public readonly int RelativeAltitude = 0;
		public readonly string[] PassengerTypes;

		public object Create(ActorInitializer init) { return new RenderCargo(init.self, this); }
	}

	public class RenderCargo : IRenderModifier
	{
		Cargo cargo;
		IFacing facing;
		IMove move;
		RenderCargoInfo Info;

		public RenderCargo(Actor self, RenderCargoInfo info)
		{
			cargo = self.Trait<Cargo>();
			facing = self.TraitOrDefault<IFacing>();
			move = self.Trait<IMove>();
			Info = info;
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<Renderable> r)
		{
			foreach (var c in cargo.Passengers)
			{
				c.Trait<ITeleportable>().SetPxPosition( c, move.PxPosition );

				var cargoFacing = c.TraitOrDefault<IFacing>();
				if (facing != null && cargoFacing != null)
					cargoFacing.Facing = facing.Facing;
			}

			var visiblePassengers = (Info.PassengerTypes != null && Info.PassengerTypes.Length > 0)
				? cargo.Passengers.Where(p =>
					Info.PassengerTypes.Contains(p.Trait<Passenger>().info.CargoType))
				: cargo.Passengers;

			return r.Concat(visiblePassengers.SelectMany(a => a.Render(wr))
				.Select(a => a.WithPxOffset(new float2(0, -Info.RelativeAltitude))
			        .WithZOffset(a.ZOffset + Info.RelativeAltitude)));
		}
	}
}