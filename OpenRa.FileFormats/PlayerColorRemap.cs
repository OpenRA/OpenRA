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
