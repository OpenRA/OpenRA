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

namespace OpenRA.Traits
{
	class HiddenUnderFogInfo : ITraitInfo
	{
		public object Create(Actor self) { return new HiddenUnderFog(self); }
	}

	class HiddenUnderFog : IRenderModifier
	{
		Shroud shroud;

		public HiddenUnderFog(Actor self)
		{
			shroud = self.World.WorldActor.traits.Get<Shroud>();
		}

		public bool IsVisible(Actor self)
		{
			return self.World.LocalPlayer == null
				|| self.Owner == self.World.LocalPlayer
				|| shroud.visibleCells[self.Location.X, self.Location.Y] > 0;
		}

		static Renderable[] Nothing = { };
		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			return IsVisible(self) ? r : Nothing;
		}
	}
}
