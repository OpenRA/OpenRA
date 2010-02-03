using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;

namespace OpenRa.Traits
{
	class PaletteFromFileInfo : ITraitInfo
	{
		public readonly string Name = "Undefined";
		public readonly string Theatre = "Undefined";
		public readonly string Filename = "";
		public readonly string Remap = "";
		public object Create(Actor self) { return new PaletteFromFile(self, this); }
	}

	class PaletteFromFile
	{
		public PaletteFromFile(Actor self, PaletteFromFileInfo info)
		{
			Log.Write("Created palette");
			if (info.Theatre == "Undefined" || 
				info.Theatre.ToLowerInvariant() == self.World.Map.Theater.ToLowerInvariant())
			{
				self.World.WorldRenderer.AddPalette(info.Name, new Palette(FileSystem.Open("temperat_ra.pal")));
			}
		}
	}
}
