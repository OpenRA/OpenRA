#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Displays a continously playing sprite overlay at the location of every occurring tile.")]
	public class TerrainTileOverlayInfo : TraitInfo, ILobbyCustomRulesIgnore
	{
		[FieldLoader.Require]
		public readonly string Tileset;

		[FieldLoader.Require]
		public readonly ushort Tile;

		[FieldLoader.Require]
		public readonly byte Index;

		[FieldLoader.Require]
		[Desc("Which image to use.")]
		public readonly string Image;

		[Desc("Which sequence to use.")]
		[SequenceReference("Image")]
		public readonly string Sequence = "idle";

		[PaletteReference]
		[Desc("Which palette to use.")]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		public override object Create(ActorInitializer init) { return new TerrainTileOverlay(init.Self, this); }
	}

	public class TerrainTileOverlay : INotifyAddedToWorld
	{
		readonly TerrainTileOverlayInfo info;
		readonly CPos[] cells;

		public TerrainTileOverlay(Actor self, TerrainTileOverlayInfo info)
		{
			this.info = info;

			var map = self.World.Map;
			cells = map.AllCells.Where(cell => info.Tile == map.Tiles[cell].Type && info.Index == map.Tiles[cell].Index).ToArray();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			var world = self.World;
			if (info.Tileset != world.Map.Tileset)
				return;

			foreach (var cell in cells)
			{
				var position = world.Map.CenterOfCell(cell);
				world.AddFrameEndTask(w => w.Add(new ContinousSpriteEffect(position, w, info.Image, info.Sequence, info.Palette, false)));
			}
		}
	}
}
