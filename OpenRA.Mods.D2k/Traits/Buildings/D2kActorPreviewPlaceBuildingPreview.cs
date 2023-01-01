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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Creates a building placement preview based on the map editor actor preview.")]
	public class D2kActorPreviewPlaceBuildingPreviewInfo : ActorPreviewPlaceBuildingPreviewInfo
	{
		[Desc("Terrain types that should show the 'unsafe' footprint tile.")]
		public readonly HashSet<string> UnsafeTerrainTypes = new HashSet<string> { "Rock" };

		[Desc("Only check for 'unsafe' footprint tiles when you have these prerequisites.")]
		public readonly string[] RequiresPrerequisites = Array.Empty<string>();

		[Desc("Sprite image to use for the overlay.")]
		public readonly string Image = "overlay";

		[SequenceReference("Image")]
		[Desc("Sprite overlay to use for valid cells.")]
		public readonly string TileValidName = "build-valid";

		[SequenceReference("Image")]
		[Desc("Sprite overlay to use for invalid cells.")]
		public readonly string TileInvalidName = "build-invalid";

		[SequenceReference("Image")]
		[Desc("Sprite overlay to use for blocked cells.")]
		public readonly string TileUnsafeName = "build-unsafe";

		protected override IPlaceBuildingPreview CreatePreview(WorldRenderer wr, ActorInfo ai, TypeDictionary init)
		{
			return new D2kActorPreviewPlaceBuildingPreviewPreview(wr, ai, this, init);
		}

		public override object Create(ActorInitializer init)
		{
			return new D2kActorPreviewPlaceBuildingPreview();
		}
	}

	public class D2kActorPreviewPlaceBuildingPreview { }

	class D2kActorPreviewPlaceBuildingPreviewPreview : ActorPreviewPlaceBuildingPreviewPreview
	{
		readonly D2kActorPreviewPlaceBuildingPreviewInfo info;
		readonly bool checkUnsafeTiles;
		readonly Sprite validTile, unsafeTile, blockedTile;
		readonly float validAlpha, unsafeAlpha, blockedAlpha;
		readonly CachedTransform<CPos, List<CPos>> unpathableCells;

		public D2kActorPreviewPlaceBuildingPreviewPreview(WorldRenderer wr, ActorInfo ai, D2kActorPreviewPlaceBuildingPreviewInfo info, TypeDictionary init)
			: base(wr, ai, info, init)
		{
			this.info = info;

			var world = wr.World;
			var sequences = world.Map.Rules.Sequences;

			var techTree = init.Get<OwnerInit>().Value(world).PlayerActor.Trait<TechTree>();
			checkUnsafeTiles = info.RequiresPrerequisites.Length > 0 && techTree.HasPrerequisites(info.RequiresPrerequisites);

			var validSequence = sequences.GetSequence(info.Image, info.TileValidName);
			validTile = validSequence.GetSprite(0);
			validAlpha = validSequence.GetAlpha(0);

			var unsafeSequence = sequences.GetSequence(info.Image, info.TileUnsafeName);
			unsafeTile = unsafeSequence.GetSprite(0);
			unsafeAlpha = unsafeSequence.GetAlpha(0);

			var blockedSequence = sequences.GetSequence(info.Image, info.TileInvalidName);
			blockedTile = blockedSequence.GetSprite(0);
			blockedAlpha = blockedSequence.GetAlpha(0);

			var buildingInfo = ai.TraitInfo<BuildingInfo>();
			unpathableCells = new CachedTransform<CPos, List<CPos>>(topLeft => buildingInfo.OccupiedTiles(topLeft).ToList());
		}

		protected override IEnumerable<IRenderable> RenderFootprint(WorldRenderer wr, CPos topLeft, Dictionary<CPos, PlaceBuildingCellType> footprint,
			PlaceBuildingCellType filter = PlaceBuildingCellType.Invalid | PlaceBuildingCellType.Valid | PlaceBuildingCellType.LineBuild)
		{
			var palette = wr.Palette(info.Palette);
			var topLeftPos = wr.World.Map.CenterOfCell(topLeft);

			var candidateSafeTiles = unpathableCells.Update(topLeft);
			foreach (var c in footprint)
			{
				if ((c.Value & filter) == 0)
					continue;

				var isUnsafe = checkUnsafeTiles && wr.World.Map.Contains(c.Key) && candidateSafeTiles.Contains(c.Key) && info.UnsafeTerrainTypes.Contains(wr.World.Map.GetTerrainInfo(c.Key).Type);
				var tile = (c.Value & PlaceBuildingCellType.Invalid) != 0 ? blockedTile : isUnsafe ? unsafeTile : validTile;
				var sequenceAlpha = (c.Value & PlaceBuildingCellType.Invalid) != 0 ? blockedAlpha : isUnsafe ? unsafeAlpha : validAlpha;

				var pos = wr.World.Map.CenterOfCell(c.Key);
				var offset = new WVec(0, 0, topLeftPos.Z - pos.Z);
				var traitAlpha = (c.Value & PlaceBuildingCellType.LineBuild) != 0 ? info.LineBuildFootprintAlpha : info.FootprintAlpha;
				yield return new SpriteRenderable(tile, pos, offset, -511, palette, 1f, sequenceAlpha * traitAlpha, float3.Ones, TintModifiers.IgnoreWorldTint, true);
			}
		}
	}
}
