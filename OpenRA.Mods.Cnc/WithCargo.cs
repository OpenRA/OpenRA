#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	public class WithCargoInfo : ITraitInfo, Requires<CargoInfo>, Requires<LocalCoordinatesModelInfo>
	{
		[Desc("Cargo position relative to turret or body. (forward, right, up) triples")]
		public readonly WRange[] LocalOffset = {};
		public readonly string[] DisplayTypes = {};

		public object Create(ActorInitializer init) { return new WithCargo(init.self, this); }
	}

	public class WithCargo : IRenderModifier
	{
		Cargo cargo;
		IFacing facing;
		WithCargoInfo Info;
		WVec[] positions;
		ILocalCoordinatesModel coords;

		public WithCargo(Actor self, WithCargoInfo info)
		{
			cargo = self.Trait<Cargo>();
			facing = self.TraitOrDefault<IFacing>();
			Info = info;

			coords = self.Trait<ILocalCoordinatesModel>();

			if (info.LocalOffset.Length % 3 != 0)
				throw new InvalidOperationException("Invalid LocalOffset array length");

			positions = new WVec[info.LocalOffset.Length / 3];
			for (var i = 0; i < info.LocalOffset.Length / 3; i++)
				positions[i] = new WVec(info.LocalOffset[3*i], info.LocalOffset[3*i + 1], info.LocalOffset[3*i + 2]);
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<Renderable> r)
		{
			foreach (var rr in r)
				yield return rr;

			var bodyOrientation = coords.QuantizeOrientation(self, self.Orientation);
			var pos = self.CenterPosition;
			int i = 0;
			foreach (var c in cargo.Passengers)
			{
				var cargoFacing = c.TraitOrDefault<IFacing>();
				if (facing != null && cargoFacing != null)
					cargoFacing.Facing = facing.Facing;

				var cargoPassenger = c.Trait<Passenger>();
				if (Info.DisplayTypes.Contains(cargoPassenger.info.CargoType))
				{
					var offset = pos - c.CenterPosition + coords.LocalToWorld(positions[i++ % positions.Length].Rotate(bodyOrientation));
					foreach (var cr in c.Render(wr))
						yield return cr.WithPos(cr.Pos + offset).WithZOffset(1);
				}
			}
		}
	}
}