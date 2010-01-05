using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace OpenRa.FileFormats
{
	public class PaletteRemap : IPaletteRemap
	{
		int offset;
		List<Color> remapColors = new List<Color>();
		Color shadowColor;

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

			offset = 80;
		}

		public PaletteRemap( Color shadowColor )
		{
			this.shadowColor = shadowColor;
		}

		public Color GetRemappedColor(Color original, int index)
		{
			if (remapColors.Count > 0)
			{
				if (index < offset || index >= offset + remapColors.Count)
					return original;

				return remapColors[index - offset];
			}

			return original.A > 0 ? shadowColor : original;
		}
	}
}
