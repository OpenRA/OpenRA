#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Loads the palette specified in the tileset definition")]
	public class PaletteFromCurrentTilesetInfo : ITraitInfo
	{
		[FieldLoader.Require, PaletteDefinition]
		[Desc("internal palette name")]
		public readonly string Name = null;

		[Desc("Map listed indices to shadow. Ignores previous color.")]
		public readonly int[] ShadowIndex = { };

		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new PaletteFromCurrentTileset(init.World, this); }
	}

	public class PaletteFromCurrentTileset : ILoadsPalettes, IProvidesAssetBrowserPalettes
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
			wr.AddPalette(info.Name, new ImmutablePalette(wr.World.Map.Open(world.Map.Rules.TileSet.Palette), info.ShadowIndex), info.AllowModifiers);
		}

		public IEnumerable<string> PaletteNames { get { yield return info.Name; } }
	}
}
