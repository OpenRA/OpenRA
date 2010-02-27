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
	class PaletteFromFileInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly string Theater = null;
		public readonly string Filename = null;
		public object Create(Actor self) { return new PaletteFromFile(self, this); }
	}

	class PaletteFromFile
	{
		public PaletteFromFile(Actor self, PaletteFromFileInfo info)
		{
			if (info.Theater == null || 
				info.Theater.ToLowerInvariant() == self.World.Map.Theater.ToLowerInvariant())
			{
				//Log.Write("Loading palette {0} from file {1}", info.Name, info.Filename);
				self.World.WorldRenderer.AddPalette(info.Name, new Palette(FileSystem.Open(info.Filename)));
			}
		}
	}
}
