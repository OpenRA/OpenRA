using System;
using System.Collections.Generic;
using System.Text;
using Ijw.DirectX;
using System.Drawing;
using System.IO;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	class HardwarePalette : Sheet
	{
		const int maxEntries = 8;
		int allocated = 0;

		public HardwarePalette(Renderer renderer, Map map, int rotate)
			: base(renderer,new Size(256, maxEntries))
		{
			Palette pal = new Palette(FileSystem.Open(map.Theater + ".pal"));
			AddPalette(pal);

			foreach (string remap in new string[] { "blue", "red", "orange", "teal", "salmon", "green", "gray" })
				AddPalette(new Palette(pal, new PaletteRemap(FileSystem.Open(remap + ".rem"))));

			using (var bitmapCopy = new Bitmap(bitmap))
				for (int j = 0; j < maxEntries; j++)
					for (int i = 0; i < 7; i++)
						this[new Point(0x60 + i, j)] = bitmapCopy.GetPixel(0x60 + (rotate + i) % 7, j);
		}

		int AddPalette(Palette p)
		{
			for (int i = 0; i < 256; i++)
				this[new Point(i, allocated)] = p.GetColor(i);

			return allocated++;
		}
	}
}
