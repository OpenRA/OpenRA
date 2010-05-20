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
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class WithShadowInfo : TraitInfo<WithShadow> {}

	class WithShadow : IRenderModifier
	{
		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			var unit = self.traits.Get<Unit>();

			var shadowSprites = r.Select(a => a.WithPalette("shadow"));
			var flyingSprites = (unit.Altitude <= 0) ? r 
				: r.Select(a => a.WithPos(a.Pos - new float2(0, unit.Altitude)).WithZOffset(3));

			return shadowSprites.Concat(flyingSprites);
		}
	}
}
