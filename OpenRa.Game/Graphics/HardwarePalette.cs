using System.Collections.Generic;
using System.Drawing;
using OpenRa.FileFormats;
using OpenRa.Traits;
using System;

namespace OpenRa.Graphics
{
	class HardwarePalette : Sheet
	{
		const int maxEntries = 32;
		int allocated = 0;
		
		// We need to store the Palettes themselves for the remap palettes to work
		// We should probably try to fix this somehow
		static Dictionary<string, Palette> palettes;
		
		public HardwarePalette(Renderer renderer, Map map)
			: base(renderer,new Size(256, maxEntries))
		{
			palettes = new Dictionary<string, Palette>();
		}
		
		public Palette GetPalette(string name)
		{
			try { return palettes[name]; }
			catch (KeyNotFoundException)
			{
				throw new InvalidOperationException(
					"Palette `{0}` does not exist".F(name));
			}
		}

		public int AddPalette(string name, Palette p)
		{
			palettes.Add(name, p);
			for (int i = 0; i < 256; i++)
			{
				this[new Point(i, allocated)] = p.GetColor(i);
			}
			return allocated++;
		}

		public void Update(IEnumerable<IPaletteModifier> paletteMods)
		{
			var b = new Bitmap(Bitmap);
			foreach (var mod in paletteMods)
				mod.AdjustPalette(b);

			Texture.SetData(b);
			Game.renderer.PaletteTexture = Texture;
		}
	}
}
