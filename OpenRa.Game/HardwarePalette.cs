using System;
using System.Collections.Generic;
using System.Text;
using BluntDirectX.Direct3D;
using System.Drawing;
using System.IO;
using OpenRa.FileFormats;

namespace OpenRa.Game
{
	class HardwarePalette : Sheet
	{
		const int maxEntries = 16;
		int allocated = 0;

		public HardwarePalette(Renderer renderer, Map map)
			: base(renderer,new Size(256, maxEntries))
		{
			Palette pal = new Palette(FileSystem.Open(map.Theater + ".pal"));
			AddPalette(pal);

			foreach (string remap in new string[] { "blue", "red", "orange", "teal", "salmon", "green", "gray" })
				AddPalette(new Palette(pal, new PaletteRemap(FileSystem.Open(remap + ".rem"))));
		}

		int AddPalette(Palette p)
		{
			for (int i = 0; i < 256; i++)
				this[new Point(i, allocated)] = p.GetColor(i);

			return allocated++;
		}
	}
}
