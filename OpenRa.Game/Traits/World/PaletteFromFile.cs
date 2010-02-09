using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;

namespace OpenRa.Traits
{
	class PaletteFromFileInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly string Theater = null;
		public readonly string Filename = null;
		public object Create(Actor self) { return new PaletteFromFile(self, this); }
	}

	class PaletteFromFile
	{
		public PaletteFromFile(Actor self, PaletteFromFileInfo info)
		{
			if (info.Theater == null || 
				info.Theater.ToLowerInvariant() == self.World.Map.Theater.ToLowerInvariant())
			{
				//Log.Write("Loading palette {0} from file {1}", info.Name, info.Filename);
				self.World.WorldRenderer.AddPalette(info.Name, new Palette(FileSystem.Open(info.Filename)));
			}
		}
	}
}
