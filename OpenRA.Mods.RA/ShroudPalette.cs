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

namespace OpenRA.Mods.RA
{
	class ShroudPaletteInfo : ITraitInfo
	{
		public readonly string Name = "shroud";
		public readonly bool IsFog = false;
		public object Create(ActorInitializer init) { return new ShroudPalette(init.world, this); }
	}

	class ShroudPalette
	{
		public ShroudPalette(World world, ShroudPaletteInfo info)
		{
				// TODO: This shouldn't rely on a base palette
				var wr = world.WorldRenderer;
				var pal = wr.GetPalette("terrain");
				wr.AddPalette(info.Name, new Palette(pal, new ShroudPaletteRemap(info.IsFog)));
		}
	}

	class ShroudPaletteRemap : IPaletteRemap
	{
		bool isFog;

		public ShroudPaletteRemap(bool isFog) { this.isFog = isFog; }
		public Color GetRemappedColor(Color original, int index)
		{
			if (isFog)
				return new[] { 
					Color.Transparent, Color.Green, 
					Color.Blue, Color.Yellow, 
					Color.FromArgb(128,0,0,0), 
					Color.FromArgb(128,0,0,0), 
					Color.FromArgb(128,0,0,0), 
					Color.FromArgb(64,0,0,0)}[index % 8];
			else
				return new[] { 
					Color.Transparent, Color.Green, 
					Color.Blue, Color.Yellow, 
					Color.Black, 
					Color.FromArgb(128,0,0,0), 
					Color.Transparent, 
					Color.Transparent}[index % 8];
		}
	}
}
