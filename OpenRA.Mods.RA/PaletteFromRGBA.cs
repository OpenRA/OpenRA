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

using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class PaletteFromRGBAInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly string Theatre = null;
		public readonly int R = 0;
		public readonly int G = 0;
		public readonly int B = 0;
		public readonly int A = 255;

		public object Create(ActorInitializer init) { return new PaletteFromRGBA(init.world, this); }
	}

	class PaletteFromRGBA
	{
		public PaletteFromRGBA(World world, PaletteFromRGBAInfo info)
		{
			if (info.Theatre == null ||
				info.Theatre.ToLowerInvariant() == world.Map.Theater.ToLowerInvariant())
			{
				// TODO: This shouldn't rely on a base palette
				var wr = world.WorldRenderer;
				var pal = wr.GetPalette("terrain");
				wr.AddPalette(info.Name, new Palette(pal, new SingleColorRemap(Color.FromArgb(info.A, info.R, info.G, info.B))));
			}
		}
	}
}
