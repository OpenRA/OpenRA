#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	class FrozenUnderFogInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new FrozenUnderFog(init.self); }
	}

	class FrozenUnderFog : IRenderModifier
	{
		Shroud shroud;
		Renderable[] cache = { };

		public FrozenUnderFog(Actor self)
		{
			shroud = self.World.WorldActor.traits.Get<Shroud>();
		}

		bool IsVisible(Actor self)
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
