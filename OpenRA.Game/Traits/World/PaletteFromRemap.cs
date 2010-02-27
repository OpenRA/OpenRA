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

using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	class PaletteFromRemapInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly string Theatre = null;
		public readonly string BasePalette = null;
		public readonly string Remap = null;
		public object Create(Actor self) { return new PaletteFromRemap(self, this); }
	}

	class PaletteFromRemap
	{
		public PaletteFromRemap(Actor self, PaletteFromRemapInfo info)
		{
			if (info.Theatre == null ||
				info.Theatre.ToLowerInvariant() == self.World.Map.Theater.ToLowerInvariant())
			{
				Log.Write("Loading palette {0} from theatre {1} with remap {2}", info.Name, info.BasePalette, info.Remap);
				var wr = self.World.WorldRenderer;
				var pal = wr.GetPalette(info.BasePalette);
				var newpal = (info.Remap == null) ? pal : new Palette(pal, new PlayerColorRemap(FileSystem.Open(info.Remap)));
				wr.AddPalette(info.Name, newpal);
			}
		}
	}
}
