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
using OpenRa.FileFormats;

namespace OpenRa.Traits
{
	class PlayerColorPaletteInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly string DisplayName = null;
		public readonly string BasePalette = null;
		public readonly string Remap = null;
		public readonly int[] DisplayColor = null;
		public object Create(Actor self) { return new PlayerColorPalette(self, this); }
	}

	class PlayerColorPalette
	{
		public PlayerColorPalette(Actor self, PlayerColorPaletteInfo info)
		{
			var wr = self.World.WorldRenderer;
			var pal = wr.GetPalette(info.BasePalette);
			var newpal = (info.Remap == null) ? pal : new Palette(pal, new PlayerColorRemap(FileSystem.Open(info.Remap)));
			wr.AddPalette(info.Name, newpal);
			Player.RegisterPlayerColor(info.Name, info.DisplayName, Color.FromArgb(info.DisplayColor[0], info.DisplayColor[1], info.DisplayColor[2]));
		}
	}
}
