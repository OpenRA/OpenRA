#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;
using OpenRA.Mods.RA;
using System;
using System.Linq;

namespace OpenRA.Mods.Cnc
{
	class RenderCargoInfo : ITraitInfo, ITraitPrerequisite<CargoInfo>
	{
		public object Create(ActorInitializer init) { return new RenderCargo(init.self); }
	}
	
	public class RenderCargo : IRenderModifier
	{
		Cargo cargo;
		
		public RenderCargo(Actor self)
		{
			cargo = self.Trait<Cargo>();
		}
		
		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			foreach (var c in cargo.Passengers)
				c.Trait<ITeleportable>().SetPxPosition( c, self.Trait<IHasLocation>().PxPosition );
			
			return r.Concat(cargo.Passengers.SelectMany(a => a.Render()));
		}
	}
}