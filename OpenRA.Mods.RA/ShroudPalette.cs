#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

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
}
