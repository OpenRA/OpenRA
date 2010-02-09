using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;

namespace OpenRa.Traits
{
	class PaletteFromRemapInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly string Theatre = null;
		public readonly string BasePalette = null;
		public readonly string Remap = null;
		public object Create(Actor self) { return new PaletteFromRemap(self, this); }
	}

	class PaletteFromRemap
	{
		public PaletteFromRemap(Actor self, PaletteFromRemapInfo info)
		{
			if (info.Theatre == null ||
				info.Theatre.ToLowerInvariant() == self.World.Map.Theater.ToLowerInvariant())
			{
				Log.Write("Loading palette {0} from theatre {1} with remap {2}", info.Name, info.BasePalette, info.Remap);
				var wr = self.World.WorldRenderer;
				var pal = wr.GetPalette(info.BasePalette);
				var newpal = (info.Remap == null) ? pal : new Palette(pal, new PlayerColorRemap(FileSystem.Open(info.Remap)));
				wr.AddPalette(info.Name, newpal);
			}
		}
	}
}
