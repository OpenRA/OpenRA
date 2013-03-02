#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class FogPaletteInfo : ITraitInfo
	{
		public readonly string Name = "fog";
		public object Create(ActorInitializer init) { return new FogPalette(this); }
	}

	class FogPalette : IPalette
	{
		readonly FogPaletteInfo info;

		public FogPalette(FogPaletteInfo info) { this.info = info; }

		public void InitPalette(WorldRenderer wr)
		{
			var c = new[] {
				Color.Transparent, Color.Green,
				Color.Blue, Color.Yellow,
				Color.FromArgb(128,0,0,0),
				Color.FromArgb(128,0,0,0),
				Color.FromArgb(128,0,0,0),
				Color.FromArgb(64,0,0,0)
			};

			wr.AddPalette(info.Name, new Palette(Exts.MakeArray(256, i => (uint)c[i % 8].ToArgb())), false);
		}
	}
}
