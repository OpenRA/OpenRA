#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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

	public interface IPaletteRemap { Color GetRemappedColor(Color original, int index);	}
}
