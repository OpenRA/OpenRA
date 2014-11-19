#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Renders the cargo loaded into the unit.")]
	public class WithCargoInfo : ITraitInfo, Requires<CargoInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Cargo position relative to turret or body. (forward, right, up) triples")]
		public readonly WVec[] LocalOffset = { };
		[Desc("Passenger CargoType to display.")]
		public readonly string[] DisplayTypes = { };

		public object Create(ActorInitializer init) { return new WithCargo(init.self, this); }
	}

	public class WithCargo : IRenderModifier
	{
		Cargo cargo;
		IFacing facing;
		WithCargoInfo cargoInfo;
		IBodyOrientation body;

		public WithCargo(Actor self, WithCargoInfo info)
		{
			cargo = self.Trait<Cargo>();
			facing = self.TraitOrDefault<IFacing>();
			cargoInfo = info;

			body = self.Trait<IBodyOrientation>();

			if (info.LocalOffset.Length == 0)
				throw new InvalidOperationException("LocalOffset must have at least one entry");
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			foreach (var rr in r)
				yield return rr;

			var bodyOrientation = body.QuantizeOrientation(self, self.Orientation);
			var pos = self.CenterPosition;
			var i = 0;
			foreach (var c in cargo.Passengers)
			{
				var cargoFacing = c.TraitOrDefault<IFacing>();
				if (facing != null && cargoFacing != null)
					cargoFacing.Facing = facing.Facing;

				var cargoPassenger = c.Trait<Passenger>();
				if (cargoInfo.DisplayTypes.Contains(cargoPassenger.Info.CargoType))
				{
					var localOffset = cargo.PassengerCount > 1 ? cargoInfo.LocalOffset[i++ % cargoInfo.LocalOffset.Length] : WVec.Zero;
					var offset = pos - c.CenterPosition + body.LocalToWorld(localOffset.Rotate(bodyOrientation));
					foreach (var cr in c.Render(wr))
						yield return cr.OffsetBy(offset).WithZOffset(1);
				}
			}
		}
	}
}