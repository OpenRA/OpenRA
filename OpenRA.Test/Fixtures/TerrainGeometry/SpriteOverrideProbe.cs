#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.TerrainProbe
{
	public sealed class SpriteOverrideProbeInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new SpriteOverrideProbe(init.World);
	}

	public sealed class SpriteOverrideProbe
#if SPRITE_API
		: ITerrainCellSpriteOverride
#endif
	{
		readonly World world;
		public bool Enabled;
		public int Calls;
		public int Replacements;
		public SpriteOverrideProbe(World world)
		{
			this.world = world;
		}
#if SPRITE_API
		Sprite ITerrainCellSpriteOverride.TerrainCellSprite(CPos cell, TerrainTile tile)
		{
			Calls++;
			if (!Enabled)
				return null!;
			Replacements++;
			return ((ITiledTerrainRenderer)world.WorldActor.Trait<TerrainRenderer>()).TileSprite(new TerrainTile(0, 0));
		}
#endif
	}

	public static class SpriteOverrideChecks
	{
		public static void Run(World world, Action<string, bool, string> check)
		{
			var provider = world.WorldActor.Trait<SpriteOverrideProbe>();
			var renderer = world.WorldActor.Trait<TerrainRenderer>();
			var cell = new CPos(52, 20);
			var tile = world.Map.Tiles[cell];
			var height = world.Map.Height[cell];
			var calls = provider.Calls;
			renderer.UpdateCell(cell);
			check("sprite-null-fallback-callback", provider.Calls == calls + 1 && provider.Replacements == 0, "");
			provider.Enabled = true;
			renderer.UpdateCell(cell);
			check("sprite-replacement-callback", provider.Replacements == 1, "");
			provider.Enabled = false;
			renderer.UpdateCell(cell);
			check("sprite-does-not-change-geometry", world.Map.Tiles[cell].Equals(tile) && world.Map.Height[cell] == height, "");
		}
	}
}
