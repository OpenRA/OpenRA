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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	public class BuildableTerrainOverlayInfo : TraitInfo
	{
		[FieldLoader.Require]
		public readonly HashSet<string> AllowedTerrainTypes = null;

		[PaletteReference]
		[Desc("Palette to use for rendering the sprite.")]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		[Desc("Sprite definition.")]
		public readonly string Image = "overlay";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence to use for unbuildable area.")]
		public readonly string Sequence = "build-invalid";

		[Desc("Custom opacity to apply to the overlay sprite.")]
		public readonly float Alpha = 1f;

		public override object Create(ActorInitializer init)
		{
			return new BuildableTerrainOverlay(init.Self, this);
		}
	}

	public class BuildableTerrainOverlay : IRenderAboveWorld, IWorldLoaded, INotifyActorDisposing
	{
		readonly BuildableTerrainOverlayInfo info;
		readonly World world;
		readonly Sprite disabledSprite;
		readonly float disabledSpriteScale;

		public bool Enabled = false;
		TerrainSpriteLayer render;
		PaletteReference palette;

		bool disposed;

		public BuildableTerrainOverlay(Actor self, BuildableTerrainOverlayInfo info)
		{
			this.info = info;
			world = self.World;

			var spriteSequence = self.World.Map.Sequences.GetSequence(info.Image, info.Sequence);
			disabledSprite = spriteSequence.GetSprite(0);
			disabledSpriteScale = spriteSequence.Scale;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			var map = world.Map;
			var mapTiles = ((IMapTiles)world.Map).Tiles;
			var mapRamp = ((IMapElevation)world.Map).Ramp;
			var m = w.Map;

			render = new TerrainSpriteLayer(w, wr, disabledSprite, BlendMode.Alpha, wr.World.Type != WorldType.Editor);

			mapTiles.CellEntryChanged += UpdateTerrainCell;
			map.CustomTerrain.CellEntryChanged += UpdateTerrainCell;

			var cells = m.AllCells.Where(c => m.Contains(c) &&
				(!info.AllowedTerrainTypes.Contains(m.GetTerrainInfo(c).Type) ||
				mapRamp[c] != 0)).ToHashSet();

			palette = wr.Palette(info.Palette);

			foreach (var cell in cells)
				UpdateTerrainCell(cell);
		}

		void UpdateTerrainCell(CPos cell)
		{
			var map = world.Map;

			if (!map.Contains(cell))
				return;

			var buildableSprite = !info.AllowedTerrainTypes.Contains(world.Map.GetTerrainInfo(cell).Type) ||
				((IMapElevation)map).Ramp[cell] != 0 ? disabledSprite : null;
			render.Update(cell, buildableSprite, palette, disabledSpriteScale, info.Alpha);
		}

		void IRenderAboveWorld.RenderAboveWorld(Actor self, WorldRenderer wr)
		{
			if (Enabled)
				render.Draw(wr.Viewport);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			render.Dispose();
			disposed = true;
		}
	}
}
