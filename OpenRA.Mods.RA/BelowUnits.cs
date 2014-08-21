#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class BelowUnitsInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new BelowUnits(init.self); }
	}

	class BelowUnits : IRenderModifier
	{
		int offset;

		public BelowUnits(Actor self)
		{
			// Offset effective position to the top of the northernmost occupied cell
			var bi = self.Info.Traits.GetOrDefault<BuildingInfo>();
			offset = ((bi != null) ? -FootprintUtils.CenterOffset(self.World, bi).Y : 0) - 512;
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return r.Select(a => a.WithZOffset(a.ZOffset + offset));
		}
	}
}
