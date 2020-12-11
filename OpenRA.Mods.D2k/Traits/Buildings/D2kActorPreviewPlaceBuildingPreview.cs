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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Creates a building placement preview based on the map editor actor preview.")]
	public class D2kActorPreviewPlaceBuildingPreviewInfo : ActorPreviewPlaceBuildingPreviewInfo
	{
		[Desc("Terrain types that should show the 'unsafe' footprint tile.")]
		public readonly HashSet<string> UnsafeTerrainTypes = new HashSet<string> { "Rock" };

		[Desc("Only check for 'unsafe' footprint tiles when you have these prerequisites.")]
		public readonly string[] RequiresPrerequisites = { };

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
		readonly Sprite buildOk;
		readonly Sprite buildUnsafe;
		readonly Sprite buildBlocked;
		readonly CachedTransform<CPos, List<CPos>> unpathableCells;

		public D2kActorPreviewPlaceBuildingPreviewPreview(WorldRenderer wr, ActorInfo ai, D2kActorPreviewPlaceBuildingPreviewInfo info, TypeDictionary init)
			: base(wr, ai, info, init)
		{
			this.info = info;

			var world = wr.World;
			var sequences = world.Map.Rules.Sequences;

			var techTree = init.Get<OwnerInit>().Value(world).PlayerActor.Trait<TechTree>();
			checkUnsafeTiles = info.RequiresPrerequisites.Any() && techTree.HasPrerequisites(info.RequiresPrerequisites);

			buildOk = sequences.GetSequence("overlay", "build-valid").GetSprite(0);
			buildUnsafe = sequences.GetSequence("overlay", "build-unsafe").GetSprite(0);
			buildBlocked = sequences.GetSequence("overlay", "build-invalid").GetSprite(0);

			var buildingInfo = ai.TraitInfo<BuildingInfo>();
			unpathableCells = new CachedTransform<CPos, List<CPos>>(topLeft => buildingInfo.OccupiedTiles(topLeft).ToList());
		}

		protected override IEnumerable<IRenderable> RenderFootprint(WorldRenderer wr, CPos topLeft, Dictionary<CPos, PlaceBuildingCellType> footprint,
			PlaceBuildingCellType filter = PlaceBuildingCellType.Invalid | PlaceBuildingCellType.Valid | PlaceBuildingCellType.LineBuild)
		{
			var cellPalette = wr.Palette(info.Palette);
			var linePalette = wr.Palette(info.LineBuildSegmentPalette);
			var topLeftPos = wr.World.Map.CenterOfCell(topLeft);

			var candidateSafeTiles = unpathableCells.Update(topLeft);
			foreach (var c in footprint)
			{
				if ((c.Value & filter) == 0)
					continue;

				var tile = HasFlag(c.Value, PlaceBuildingCellType.Invalid) ? buildBlocked :
					(checkUnsafeTiles && candidateSafeTiles.Contains(c.Key) && info.UnsafeTerrainTypes.Contains(wr.World.Map.GetTerrainInfo(c.Key).Type))
					? buildUnsafe : buildOk;

				var pal = HasFlag(c.Value, PlaceBuildingCellType.LineBuild) ? linePalette : cellPalette;
				var pos = wr.World.Map.CenterOfCell(c.Key);
				var offset = new WVec(0, 0, topLeftPos.Z - pos.Z);
				yield return new SpriteRenderable(tile, pos, offset, -511, pal, 1f, true, TintModifiers.IgnoreWorldTint);
			}
		}
	}
}
