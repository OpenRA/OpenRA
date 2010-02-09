using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenRa.FileFormats;

namespace OpenRa.Traits
{
	class PaletteFromRGBAInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly string Theatre = null;
		public readonly int R = 0;
		public readonly int G = 0;
		public readonly int B = 0;
		public readonly int A = 255;
		public object Create(Actor self) { return new PaletteFromRGBA(self, this); }
	}

	class PaletteFromRGBA
	{
		public PaletteFromRGBA(Actor self, PaletteFromRGBAInfo info)
		{
			if (info.Theatre == null ||
				info.Theatre.ToLowerInvariant() == self.World.Map.Theater.ToLowerInvariant())
			{
				Log.Write("Loading palette {0} from RGBA {1} {2} {3} {4}",info.Name,info.R,info.G,info.B,info.A);
				// TODO: This shouldn't rely on a base palette
				var wr = self.World.WorldRenderer;
				var pal = wr.GetPalette("player0");
				wr.AddPalette(info.Name, new Palette(pal, new SingleColorRemap(Color.FromArgb(info.A, info.R, info.G, info.B))));
			}
		}
	}
}
