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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Creates a building placement preview based on the map editor actor preview.")]
	public class ActorPreviewPlaceBuildingPreviewInfo : FootprintPlaceBuildingPreviewInfo
	{
		[Desc("Enable the building's idle animation.")]
		public readonly bool Animated = true;

		[PaletteReference(nameof(OverridePaletteIsPlayerPalette))]
		[Desc("Custom palette name.")]
		public readonly string OverridePalette = null;

		[Desc("Custom palette is a player palette BaseName.")]
		public readonly bool OverridePaletteIsPlayerPalette = true;

		[Desc("Footprint types to draw underneath the actor preview.")]
		public readonly PlaceBuildingCellType FootprintUnderPreview = PlaceBuildingCellType.Valid | PlaceBuildingCellType.LineBuild;

		[Desc("Footprint types to draw above the actor preview.")]
		public readonly PlaceBuildingCellType FootprintOverPreview = PlaceBuildingCellType.Invalid;

		protected override IPlaceBuildingPreview CreatePreview(WorldRenderer wr, ActorInfo ai, TypeDictionary init)
		{
			return new ActorPreviewPlaceBuildingPreviewPreview(wr, ai, this, init);
		}

		public override object Create(ActorInitializer init)
		{
			return new ActorPreviewPlaceBuildingPreview();
		}
	}

	public class ActorPreviewPlaceBuildingPreview { }

	public class ActorPreviewPlaceBuildingPreviewPreview : FootprintPlaceBuildingPreviewPreview
	{
		readonly ActorPreviewPlaceBuildingPreviewInfo info;
		readonly PaletteReference palette;
		readonly IActorPreview[] preview;

		public ActorPreviewPlaceBuildingPreviewPreview(WorldRenderer wr, ActorInfo ai, ActorPreviewPlaceBuildingPreviewInfo info, TypeDictionary init)
			: base(wr, ai, info, init)
		{
			this.info = info;
			var previewInit = new ActorPreviewInitializer(actorInfo, wr, init);
			preview = actorInfo.TraitInfos<IRenderActorPreviewInfo>()
				.SelectMany(rpi => rpi.RenderPreview(previewInit))
				.ToArray();

			if (!string.IsNullOrEmpty(info.OverridePalette))
			{
				var ownerName = init.Get<OwnerInit>().InternalName;
				palette = wr.Palette(info.OverridePaletteIsPlayerPalette ? info.OverridePalette + ownerName : info.OverridePalette);
			}
		}

		protected override void TickInner()
		{
			if (!info.Animated)
				return;

			foreach (var p in preview)
				p.Tick();
		}

		protected override IEnumerable<IRenderable> RenderInner(WorldRenderer wr, CPos topLeft, Dictionary<CPos, PlaceBuildingCellType> footprint)
		{
			var centerPosition = wr.World.Map.CenterOfCell(topLeft) + centerOffset;
			var previewRenderables = preview
				.SelectMany(p => p.Render(wr, centerPosition));

			if (palette != null)
				previewRenderables = previewRenderables.Select(a => !a.IsDecoration && a is IPalettedRenderable ? ((IPalettedRenderable)a).WithPalette(palette) : a);

			if (info.FootprintUnderPreview != PlaceBuildingCellType.None)
				foreach (var r in RenderFootprint(wr, topLeft, footprint, info.FootprintUnderPreview))
					yield return r;

			foreach (var r in previewRenderables.OrderBy(WorldRenderer.RenderableZPositionComparisonKey))
				yield return r;

			if (info.FootprintOverPreview != PlaceBuildingCellType.None)
				foreach (var r in RenderFootprint(wr, topLeft, footprint, info.FootprintOverPreview))
					yield return r;
		}
	}
}
