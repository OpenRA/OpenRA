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
using System.Drawing;
using System.IO;

namespace OpenRa.FileFormats
{
	public class PlayerColorRemap : IPaletteRemap
	{
		int offset;
		List<Color> remapColors = new List<Color>();

		public PlayerColorRemap(Stream s)
		{
			using (BinaryReader reader = new BinaryReader(s))
			{
				for (int i = 0; i < 16; i++)
				{
					byte r = reader.ReadByte();
					byte g = reader.ReadByte();
					byte b = reader.ReadByte();

					remapColors.Add(Color.FromArgb(r, g, b));
				}
			}

			offset = 80;
		}

		public Color GetRemappedColor(Color original, int index)
		{
			if (index < offset || index >= offset + remapColors.Count)
				return original;

			return remapColors[index - offset];
		}
	}
}
