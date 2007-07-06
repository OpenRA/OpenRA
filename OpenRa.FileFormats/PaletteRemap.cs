using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace OpenRa.FileFormats
{
	public class PaletteRemap
	{
		List<Color> remapColors = new List<Color>();

		public PaletteRemap(Stream s)
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
		}

		public Color GetRemappedColor(Color original, int index)
		{
			if (index < 80 || index >= 96)
				return original;

			return remapColors[index - 80];
		}
	}
}
