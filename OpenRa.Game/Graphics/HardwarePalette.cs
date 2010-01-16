using System.Drawing;
using OpenRa.FileFormats;

namespace OpenRa.Graphics
{
	public enum PaletteType
	{
		Gold, Blue, Red, Orange, Teal, Salmon, Green, Gray,
		Shadow, Invuln, Disabled, Highlight, Shroud, Chrome, 
	};

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
				AddPalette(new Palette(pal, new PlayerColorRemap(FileSystem.Open(remap + ".rem"))));

			AddPalette(new Palette(pal, new SingleColorRemap(Color.FromArgb(140, 0, 0, 0))));		// Shadow
			AddPalette(new Palette(pal, new SingleColorRemap(Color.FromArgb(128, 128, 0, 0))));		// Invulnerable (Iron Curtain)
			AddPalette(new Palette(pal, new SingleColorRemap(Color.FromArgb(180, 0, 0, 0))));		// Disabled / Low power
			AddPalette(new Palette(pal, new SingleColorRemap(Color.FromArgb(128, 255, 255, 255))));	// Highlight
			AddPalette(new Palette(pal, new ShroudPaletteRemap()));									// Shroud
			AddPalette(pal);	// Chrome (it's like gold, but we're not going to hax it in palettemods)
		}

		int AddPalette(Palette p)
		{
			for (int i = 0; i < 256; i++)
				this[new Point(i, allocated)] = p.GetColor(i);

			return allocated++;
		}
	}
}
