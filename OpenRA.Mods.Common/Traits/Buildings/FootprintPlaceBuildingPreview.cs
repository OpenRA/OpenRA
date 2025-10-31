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

		[Desc("Custom opacity to apply to the placement sprite.")]
		public readonly float FootprintAlpha = 1f;

		[Desc("Custom opacity to apply to the line-build placement sprite.")]
		public readonly float LineBuildFootprintAlpha = 1f;

		protected virtual IPlaceBuildingPreview CreatePreview(WorldRenderer wr, ActorInfo ai, TypeDictionary init)
		{
			return new FootprintPlaceBuildingPreviewPreview(wr, ai, this);
		}

		IPlaceBuildingPreview IPlaceBuildingPreviewGeneratorInfo.CreatePreview(WorldRenderer wr, ActorInfo ai, TypeDictionary init)
		{
			return CreatePreview(wr, ai, init);
		}
	}

	public class FootprintPlaceBuildingPreview { }

	public class FootprintPlaceBuildingPreviewPreview : IPlaceBuildingPreview
	{
		protected readonly ActorInfo ActorInfo;
		protected readonly WVec CenterOffset;
		readonly FootprintPlaceBuildingPreviewInfo info;
		readonly IPlaceBuildingDecorationInfo[] decorations;
		readonly int2 topLeftScreenOffset;
		readonly Sprite validTile, blockedTile;
		readonly float validAlpha, blockedAlpha;

		public FootprintPlaceBuildingPreviewPreview(WorldRenderer wr, ActorInfo ai, FootprintPlaceBuildingPreviewInfo info)
		{
			ActorInfo = ai;
			this.info = info;
			decorations = ActorInfo.TraitInfos<IPlaceBuildingDecorationInfo>().ToArray();

			var world = wr.World;
			CenterOffset = ActorInfo.TraitInfo<BuildingInfo>().CenterOffset(world);
			topLeftScreenOffset = -wr.ScreenPxOffset(CenterOffset);

			var tileset = world.Map.Tileset.ToLowerInvariant();
			var sequences = world.Map.Sequences;
			if (sequences.HasSequence("overlay", $"build-valid-{tileset}"))
			{
				var validSequence = sequences.GetSequence("overlay", $"build-valid-{tileset}");
				validTile = validSequence.GetSprite(0);
				validAlpha = validSequence.GetAlpha(0);
			}
			else
			{
				var validSequence = sequences.GetSequence("overlay", "build-valid");
				validTile = validSequence.GetSprite(0);
				validAlpha = validSequence.GetAlpha(0);
			}

			var blockedSequence = sequences.GetSequence("overlay", "build-invalid");
			blockedTile = blockedSequence.GetSprite(0);
			blockedAlpha = blockedSequence.GetAlpha(0);
		}

		protected virtual void TickInner() { }

		protected virtual IEnumerable<IRenderable> RenderFootprint(WorldRenderer wr, CPos topLeft, Dictionary<CPos, PlaceBuildingCellType> footprint,
			PlaceBuildingCellType filter = PlaceBuildingCellType.Invalid | PlaceBuildingCellType.Valid | PlaceBuildingCellType.LineBuild)
		{
			var palette = wr.Palette(info.Palette);
			var topLeftPos = wr.World.Map.CenterOfCell(topLeft);
			foreach (var c in footprint)
			{
				if ((c.Value & filter) == 0)
					continue;

				var tile = (c.Value & PlaceBuildingCellType.Invalid) != 0 ? blockedTile : validTile;
				var sequenceAlpha = (c.Value & PlaceBuildingCellType.Invalid) != 0 ? blockedAlpha : validAlpha;
				var pos = wr.World.Map.CenterOfCell(c.Key);
				var offset = new WVec(0, 0, topLeftPos.Z - pos.Z);
				var traitAlpha = (c.Value & PlaceBuildingCellType.LineBuild) != 0 ? info.LineBuildFootprintAlpha : info.FootprintAlpha;
				yield return new SpriteRenderable(tile, pos, offset, -511, palette, 1f, sequenceAlpha * traitAlpha, float3.Ones, TintModifiers.IgnoreWorldTint, true);
			}
		}

		protected virtual IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, CPos topLeft)
		{
			var centerPosition = wr.World.Map.CenterOfCell(topLeft) + CenterOffset;
			foreach (var d in decorations)
				foreach (var r in d.RenderAnnotations(wr, wr.World, ActorInfo, centerPosition))
					yield return r;
		}

		protected virtual IEnumerable<IRenderable> RenderInner(WorldRenderer wr, CPos topLeft, Dictionary<CPos, PlaceBuildingCellType> footprint)
		{
			return RenderFootprint(wr, topLeft, footprint);
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

		int2 IPlaceBuildingPreview.TopLeftScreenOffset => topLeftScreenOffset;
	}
}
