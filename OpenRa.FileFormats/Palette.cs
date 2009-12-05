using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace OpenRa.FileFormats
{
	public class Palette
	{
		List<Color> colors = new List<Color>();

		public Color GetColor(int index)
		{
			return colors[index];
		}

		public Palette(Stream s)
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
			colors[3] = Color.FromArgb(178, 0, 0, 0);
			colors[4] = Color.FromArgb(140, 0, 0, 0);
		}

		public Palette(Palette p, PaletteRemap r)
		{
			for (int i = 0; i < 256; i++)
				colors.Add(r.GetRemappedColor(p.GetColor(i), i));
		}
	}
}
