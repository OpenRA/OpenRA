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

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class PlayerPaletteFromCurrentTilesetInfo : ITraitInfo
	{
		[FieldLoader.Require, PaletteDefinition(true)]
		[Desc("internal palette name")]
		public readonly string Name = null;
		[Desc("Map listed indices to shadow.")]
		public readonly int[] ShadowIndex = { };
		[Desc("Apply palette rotators or not.")]
		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new PlayerPaletteFromCurrentTileset(init.World, this); }
	}

	class PlayerPaletteFromCurrentTileset : ILoadsPalettes
	{
		readonly World world;
		readonly PlayerPaletteFromCurrentTilesetInfo info;

		public PlayerPaletteFromCurrentTileset(World world, PlayerPaletteFromCurrentTilesetInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void LoadPalettes(WorldRenderer wr)
		{
			var filename = world.Map.Rules.TileSet.PlayerPalette ?? world.Map.Rules.TileSet.Palette;
			wr.AddPalette(info.Name, new ImmutablePalette(wr.World.Map.Open(filename), info.ShadowIndex), info.AllowModifiers);
		}
	}
}
