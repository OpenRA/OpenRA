#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	[Desc("Loads the palette specified in the tileset definition")]
	class PaletteFromCurrentTilesetInfo : ITraitInfo
	{
		[Desc("internal palette name")]
		public readonly string Name = null;
		[Desc("Map listed indices to shadow. Ignores previous color.")]
		public readonly int[] ShadowIndex = { };
		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new PaletteFromCurrentTileset(init.world, this); }
	}

	class PaletteFromCurrentTileset : ILoadsPalettes
	{
		readonly World world;
		readonly PaletteFromCurrentTilesetInfo info;

		public PaletteFromCurrentTileset(World world, PaletteFromCurrentTilesetInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void LoadPalettes(WorldRenderer wr)
		{
			wr.AddPalette(info.Name, new ImmutablePalette(GlobalFileSystem.Open(world.TileSet.Palette), info.ShadowIndex), info.AllowModifiers);
		}
	}
}
