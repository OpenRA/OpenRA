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
using OpenRa.Traits;

namespace OpenRa.Mods.RA
{
	class RenderSpyInfo : RenderInfantryInfo
	{
		public override object Create(Actor self) { return new RenderSpy(self); }
	}

	class RenderSpy : RenderInfantry, IRenderModifier
	{
		public RenderSpy(Actor self)
			: base(self)
		{
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			if (self.Owner == self.World.LocalPlayer)
				return r;

			return r.Select(a => a.WithPalette(self.World.LocalPlayer.Palette));
		}

		public override void Tick(Actor self)
		{
			anim.ChangeImage(self.Owner == self.World.LocalPlayer ? GetImage(self) : "e1");
			base.Tick(self);
		}
	}
}
