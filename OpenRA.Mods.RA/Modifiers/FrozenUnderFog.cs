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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class FrozenUnderFogInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new FrozenUnderFog(init.self); }
	}

	class FrozenUnderFog : IRenderModifier, IVisibilityModifier
	{
		Shroud shroud;
		Renderable[] cache = { };

		public FrozenUnderFog(Actor self)
		{
			shroud = self.World.WorldActor.Trait<Shroud>();
		}

		public bool IsVisible(Actor self)
		{
			return self.World.LocalPlayer == null
				|| self.Owner == self.World.LocalPlayer
				|| self.World.LocalPlayer.Shroud.Disabled
				|| Shroud.GetVisOrigins(self).Any(o => self.World.Map.IsInMap(o) && shroud.visibleCells[o.X, o.Y] != 0);
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			if (IsVisible(self))
				cache = r.ToArray();

			return cache;
		}
	}
}
