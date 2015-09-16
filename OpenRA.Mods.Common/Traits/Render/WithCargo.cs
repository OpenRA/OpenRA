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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Renders the cargo loaded into the unit.")]
	public class WithCargoInfo : ITraitInfo, Requires<CargoInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Cargo position relative to turret or body in (forward, right, up) triples. The default offset should be in the middle of the list.")]
		public readonly WVec[] LocalOffset = { WVec.Zero };

		[Desc("Passenger CargoType to display.")]
		public readonly HashSet<string> DisplayTypes = new HashSet<string>();

		public object Create(ActorInitializer init) { return new WithCargo(init.Self, this); }
	}

	public class WithCargo : IRenderModifier, ITick, INotifyPassengerEntered, INotifyPassengerExited
	{
		readonly Cargo cargo;
		readonly IFacing facing;
		readonly WithCargoInfo cargoInfo;
		readonly IBodyOrientation body;

		readonly List<IFacing> passengerFacings;
		readonly Dictionary<Actor, PassengerInfo> passengerInfos;

		public WithCargo(Actor self, WithCargoInfo info)
		{
			cargo = self.Trait<Cargo>();
			facing = self.TraitOrDefault<IFacing>();
			cargoInfo = info;

			body = self.Trait<IBodyOrientation>();

			passengerFacings = new List<IFacing>();
			passengerInfos = new Dictionary<Actor, PassengerInfo>();
		}

		public void PassengerEntered(Actor self, Actor passenger)
		{
			var passengerFacing = passenger.TraitOrDefault<IFacing>();
			if (passengerFacing != null)
				passengerFacings.Add(passengerFacing);

			var passengerInfo = passenger.Trait<Passenger>().Info;
			if (!passengerInfos.ContainsKey(passenger))
				passengerInfos.Add(passenger, passengerInfo);
		}

		public void PassengerExited(Actor self, Actor passenger)
		{
			var passengerFacing = passenger.TraitOrDefault<IFacing>();
			if (passengerFacing != null)
				passengerFacings.Remove(passengerFacing);

			passengerInfos.Remove(passenger);
		}

		public void Tick(Actor self)
		{
			if (facing == null)
				return;

			foreach (var passengerFacing in passengerFacings)
				passengerFacing.Facing = facing.Facing;
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
				if (cargoInfo.DisplayTypes.Contains(passengerInfos[c].CargoType))
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