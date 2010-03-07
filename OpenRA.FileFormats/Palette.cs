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

namespace OpenRA.FileFormats
{
	public class Palette
	{
		List<Color> colors = new List<Color>();

		public Color GetColor(int index)
		{
			return colors[index];
		}

		public Palette(Stream s, bool remapTransparent)
		{
			using (BinaryReader reader = new BinaryReader(s))
			{
				for (int i = 0; i < 256; i++)
				{
					byte r = (byte)(reader.ReadByte() << 2);
					byte g = (byte)(reader.ReadByte() << 2);
					byte b = (byte)(reader.ReadByte() << 2);

					colors.Add(Color.FromArgb(r, g, b));
				}
			}

			colors[0] = Color.FromArgb(0, 0, 0, 0);

			if (remapTransparent)
			{
				colors[3] = Color.FromArgb(178, 0, 0, 0);
				colors[4] = Color.FromArgb(140, 0, 0, 0);
			}
		}

		public Palette(Palette p, IPaletteRemap r)
		{
			for (int i = 0; i < 256; i++)
				colors.Add(r.GetRemappedColor(p.GetColor(i), i));
		}
	}
}
