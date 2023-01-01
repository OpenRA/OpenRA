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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Creates a building placement preview based on a defined sequence.")]
	public class SequencePlaceBuildingPreviewInfo : FootprintPlaceBuildingPreviewInfo, Requires<RenderSpritesInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use.")]
		public readonly string Sequence = "idle";

		[Desc("Custom opacity to apply to the sequence sprite.")]
		public readonly float SequenceAlpha = 1f;

		[Desc("Footprint types to draw underneath the actor preview.")]
		public readonly PlaceBuildingCellType FootprintUnderPreview = PlaceBuildingCellType.Valid | PlaceBuildingCellType.LineBuild;

		[Desc("Footprint types to draw above the actor preview.")]
		public readonly PlaceBuildingCellType FootprintOverPreview = PlaceBuildingCellType.Invalid;

		protected override IPlaceBuildingPreview CreatePreview(WorldRenderer wr, ActorInfo ai, TypeDictionary init)
		{
			return new SequencePlaceBuildingPreviewPreview(wr, ai, this, init);
		}

		public override object Create(ActorInitializer init)
		{
			return new SequencePlaceBuildingPreview();
		}
	}

	public class SequencePlaceBuildingPreview { }

	class SequencePlaceBuildingPreviewPreview : FootprintPlaceBuildingPreviewPreview
	{
		readonly SequencePlaceBuildingPreviewInfo info;
		readonly Animation preview;
		readonly PaletteReference palette;

		public SequencePlaceBuildingPreviewPreview(WorldRenderer wr, ActorInfo ai, SequencePlaceBuildingPreviewInfo info, TypeDictionary init)
			: base(wr, ai, info)
		{
			this.info = info;
			var ownerName = init.Get<OwnerInit>().InternalName;
			var faction = init.Get<FactionInit>().Value;

			var rsi = ai.TraitInfo<RenderSpritesInfo>();
			palette = wr.Palette(rsi.Palette ?? rsi.PlayerPalette + ownerName);
			preview = new Animation(wr.World, rsi.GetImage(ai, faction));
			preview.PlayRepeating(info.Sequence);
		}

		protected override void TickInner()
		{
			preview.Tick();
		}

		protected override IEnumerable<IRenderable> RenderInner(WorldRenderer wr, CPos topLeft, Dictionary<CPos, PlaceBuildingCellType> footprint)
		{
			if (info.FootprintUnderPreview != PlaceBuildingCellType.None)
				foreach (var r in RenderFootprint(wr, topLeft, footprint, info.FootprintUnderPreview))
					yield return r;

			var centerPosition = wr.World.Map.CenterOfCell(topLeft) + CenterOffset;
			foreach (var r in preview.Render(centerPosition, WVec.Zero, 0, palette))
			{
				if (info.SequenceAlpha < 1f && r is IModifyableRenderable mr)
					yield return mr.WithAlpha(mr.Alpha * info.SequenceAlpha);
				else
					yield return r;
			}

			if (info.FootprintOverPreview != PlaceBuildingCellType.None)
				foreach (var r in RenderFootprint(wr, topLeft, footprint, info.FootprintOverPreview))
					yield return r;
		}
	}
}
