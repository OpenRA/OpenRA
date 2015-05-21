#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Renders the cargo loaded into the unit.")]
	public class WithCargoInfo : ITraitInfo, Requires<CargoInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Cargo position relative to turret or body in (forward, right, up) triples. The default offset should be in the middle of the list.")]
		public readonly WVec[] LocalOffset = { WVec.Zero };

		[Desc("Passenger CargoType to display.")]
		public readonly string[] DisplayTypes = { };

		public object Create(ActorInitializer init) { return new WithCargo(init.Self, this); }
	}

	public class WithCargo : IRenderModifier
	{
		readonly Cargo cargo;
		readonly IFacing facing;
		readonly WithCargoInfo cargoInfo;
		readonly IBodyOrientation body;

		public WithCargo(Actor self, WithCargoInfo info)
		{
			cargo = self.Trait<Cargo>();
			facing = self.TraitOrDefault<IFacing>();
			cargoInfo = info;

			body = self.Trait<IBodyOrientation>();
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
					var index = cargo.PassengerCount > 1 ? i++ % cargoInfo.LocalOffset.Length : cargoInfo.LocalOffset.Length / 2;
					var localOffset = cargoInfo.LocalOffset[index];
					var offset = pos - c.CenterPosition + body.LocalToWorld(localOffset.Rotate(bodyOrientation));
					foreach (var cr in c.Render(wr))
						yield return cr.OffsetBy(offset).WithZOffset(1);
				}
			}
		}
	}
}