using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenRa.FileFormats;

namespace OpenRa.Traits
{
	class PlayerColorPaletteInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly string DisplayName = null;
		public readonly string BasePalette = null;
		public readonly string Remap = null;
		public readonly int[] DisplayColor = null;
		public object Create(Actor self) { return new PlayerColorPalette(self, this); }
	}

	class PlayerColorPalette
	{
		public PlayerColorPalette(Actor self, PlayerColorPaletteInfo info)
		{
			var wr = self.World.WorldRenderer;
			var pal = wr.GetPalette(info.BasePalette);
			var newpal = (info.Remap == null) ? pal : new Palette(pal, new PlayerColorRemap(FileSystem.Open(info.Remap)));
			wr.AddPalette(info.Name, newpal);
			Player.RegisterPlayerColor(info.Name, info.DisplayName, Color.FromArgb(info.DisplayColor[0], info.DisplayColor[1], info.DisplayColor[2]));
		}
	}
}
