#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA
{
	class PaletteFromRGBAInfo : ITraitInfo
	{
		public readonly string Name = null;
		public readonly string Tileset = null;
		public readonly int R = 0;
		public readonly int G = 0;
		public readonly int B = 0;
		public readonly int A = 255;

		public object Create(ActorInitializer init) { return new PaletteFromRGBA(init.world, this); }
	}

	class PaletteFromRGBA : IPalette
	{
		readonly World world;
		readonly PaletteFromRGBAInfo info;
		public PaletteFromRGBA(World world, PaletteFromRGBAInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void InitPalette( WorldRenderer wr )
		{
			if (info.Tileset == null || info.Tileset.ToLowerInvariant() == world.Map.Tileset.ToLowerInvariant())
			{
				// TODO: This shouldn't rely on a base palette
				var pal = wr.GetPalette("terrain");
				wr.AddPalette(info.Name, new Palette(pal, new SingleColorRemap(Color.FromArgb(info.A, info.R, info.G, info.B))));
			}
		}
	}

	class SingleColorRemap : IPaletteRemap
	{
		Color c;
		public SingleColorRemap(Color c)
		{
			this.c = c;
		}

		public Color GetRemappedColor(Color original, int index)
		{
			return original.A > 0 ? c : original;
		}
	}
}
