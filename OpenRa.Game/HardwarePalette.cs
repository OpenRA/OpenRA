using System;
using System.Collections.Generic;
using System.Text;
using BluntDirectX.Direct3D;
using System.Drawing;
using System.IO;
using OpenRa.FileFormats;

namespace OpenRa.Game
{
	class HardwarePalette
	{
		const int maxEntries = 16;			// dont need anything like this many, 
											// but the hardware likes square textures better

		Bitmap bitmap = new Bitmap(256, maxEntries);
		GraphicsDevice device;
		int allocated = 0;

		Texture paletteTexture;

		public HardwarePalette(GraphicsDevice device)
		{
			this.device = device;
		}

		public void Resolve()
		{
			const string filename = "../../../palette-cache.png";
			bitmap.Save(filename);

			using (Stream s = File.OpenRead(filename))
				paletteTexture = Texture.Create(s, device);
		}

		public Texture PaletteTexture
		{
			get
			{
				if (paletteTexture == null)
					Resolve();

				return paletteTexture;
			}
		}

		public int AddPalette(Palette p)
		{
			for (int i = 0; i < 256; i++)
				bitmap.SetPixel(i, allocated, p.GetColor(i));

			return allocated++;
		}
	}
}
