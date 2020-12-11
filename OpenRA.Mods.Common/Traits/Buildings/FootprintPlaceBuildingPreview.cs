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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Creates a building placement preview showing only the building footprint.")]
	public class FootprintPlaceBuildingPreviewInfo : TraitInfo<FootprintPlaceBuildingPreview>, IPlaceBuildingPreviewGeneratorInfo
	{
		[PaletteReference]
		[Desc("Palette to use for rendering the placement sprite.")]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		[PaletteReference]
		[Desc("Palette to use for rendering the placement sprite for line build segments.")]
		public readonly string LineBuildSegmentPalette = TileSet.TerrainPaletteInternalName;

		protected virtual IPlaceBuildingPreview CreatePreview(WorldRenderer wr, ActorInfo ai, TypeDictionary init)
		{
			return new FootprintPlaceBuildingPreviewPreview(wr, ai, this, init);
		}

		IPlaceBuildingPreview IPlaceBuildingPreviewGeneratorInfo.CreatePreview(WorldRenderer wr, ActorInfo ai, TypeDictionary init)
		{
			return CreatePreview(wr, ai, init);
		}
	}

	public class FootprintPlaceBuildingPreview { }

	public class FootprintPlaceBuildingPreviewPreview : IPlaceBuildingPreview
	{
		protected readonly ActorInfo actorInfo;
		protected readonly WVec centerOffset;
		readonly FootprintPlaceBuildingPreviewInfo info;
		readonly IPlaceBuildingDecorationInfo[] decorations;
		readonly int2 topLeftScreenOffset;
		readonly Sprite buildOk;
		readonly Sprite buildBlocked;

		protected static bool HasFlag(PlaceBuildingCellType value, PlaceBuildingCellType flag)
		{
			// PERF: Enum.HasFlag is slower and requires allocations.
			return (value & flag) == value;
		}

		public FootprintPlaceBuildingPreviewPreview(WorldRenderer wr, ActorInfo ai, FootprintPlaceBuildingPreviewInfo info, TypeDictionary init)
		{
			actorInfo = ai;
			this.info = info;
			decorations = actorInfo.TraitInfos<IPlaceBuildingDecorationInfo>().ToArray();

			var world = wr.World;
			centerOffset = actorInfo.TraitInfo<BuildingInfo>().CenterOffset(world);
			topLeftScreenOffset = -wr.ScreenPxOffset(centerOffset);

			var tileset = world.Map.Tileset.ToLowerInvariant();
			if (world.Map.Rules.Sequences.HasSequence("overlay", "build-valid-{0}".F(tileset)))
				buildOk = world.Map.Rules.Sequences.GetSequence("overlay", "build-valid-{0}".F(tileset)).GetSprite(0);
			else
				buildOk = world.Map.Rules.Sequences.GetSequence("overlay", "build-valid").GetSprite(0);
			buildBlocked = world.Map.Rules.Sequences.GetSequence("overlay", "build-invalid").GetSprite(0);
		}

		protected virtual void TickInner() { }

		protected virtual IEnumerable<IRenderable> RenderFootprint(WorldRenderer wr, CPos topLeft, Dictionary<CPos, PlaceBuildingCellType> footprint,
			PlaceBuildingCellType filter = PlaceBuildingCellType.Invalid | PlaceBuildingCellType.Valid | PlaceBuildingCellType.LineBuild)
		{
			var cellPalette = wr.Palette(info.Palette);
			var linePalette = wr.Palette(info.LineBuildSegmentPalette);
			var topLeftPos = wr.World.Map.CenterOfCell(topLeft);
			foreach (var c in footprint)
			{
				if ((c.Value & filter) == 0)
					continue;

				var tile = HasFlag(c.Value, PlaceBuildingCellType.Invalid) ? buildBlocked : buildOk;
				var pal = HasFlag(c.Value, PlaceBuildingCellType.LineBuild) ? linePalette : cellPalette;
				var pos = wr.World.Map.CenterOfCell(c.Key);
				var offset = new WVec(0, 0, topLeftPos.Z - pos.Z);
				yield return new SpriteRenderable(tile, pos, offset, -511, pal, 1f, true, TintModifiers.IgnoreWorldTint);
			}
		}

		protected virtual IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, CPos topLeft)
		{
			var centerPosition = wr.World.Map.CenterOfCell(topLeft) + centerOffset;
			foreach (var d in decorations)
				foreach (var r in d.RenderAnnotations(wr, wr.World, actorInfo, centerPosition))
					yield return r;
		}

		protected virtual IEnumerable<IRenderable> RenderInner(WorldRenderer wr, CPos topLeft, Dictionary<CPos, PlaceBuildingCellType> footprint)
		{
			foreach (var r in RenderFootprint(wr, topLeft, footprint))
				yield return r;
		}

		IEnumerable<IRenderable> IPlaceBuildingPreview.Render(WorldRenderer wr, CPos topLeft, Dictionary<CPos, PlaceBuildingCellType> footprint)
		{
			return RenderInner(wr, topLeft, footprint);
		}

		IEnumerable<IRenderable> IPlaceBuildingPreview.RenderAnnotations(WorldRenderer wr, CPos topLeft)
		{
			return RenderAnnotations(wr, topLeft);
		}

		void IPlaceBuildingPreview.Tick() { TickInner(); }

		int2 IPlaceBuildingPreview.TopLeftScreenOffset { get { return topLeftScreenOffset; } }
	}
}
