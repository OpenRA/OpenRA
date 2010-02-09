using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenRa.FileFormats;

namespace OpenRa.Traits
{
	class ShroudPaletteInfo : ITraitInfo
	{
		public object Create(Actor self) { return new ShroudPalette(self); }
	}

	class ShroudPalette
	{
		public ShroudPalette(Actor self)
		{
				// TODO: This shouldn't rely on a base palette
				var wr = self.World.WorldRenderer;
				var pal = wr.GetPalette("terrain");

				wr.AddPalette("shroud", new Palette(pal, new ShroudPaletteRemap()));
		}
	}
}
