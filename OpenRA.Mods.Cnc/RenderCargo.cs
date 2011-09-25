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
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	class RenderCargoInfo : ITraitInfo, Requires<CargoInfo>
	{
		public object Create(ActorInitializer init) { return new RenderCargo(init.self); }
	}

	public class RenderCargo : IRenderModifier
	{
		Cargo cargo;
		IFacing facing;

		public RenderCargo(Actor self)
		{
			cargo = self.Trait<Cargo>();
			facing = self.TraitOrDefault<IFacing>();
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			foreach (var c in cargo.Passengers)
			{
				c.Trait<ITeleportable>().SetPxPosition( c, self.Trait<IHasLocation>().PxPosition );
				if (facing != null && c.HasTrait<IFacing>())
					c.Trait<IFacing>().Facing = facing.Facing;
			}
			return r.Concat(cargo.Passengers.SelectMany(a => a.Render()));
		}
	}
}