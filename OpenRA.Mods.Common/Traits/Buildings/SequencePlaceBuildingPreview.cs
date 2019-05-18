#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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

		[PaletteReference("SequencePaletteIsPlayerPalette")]
		[Desc("Custom palette name.")]
		public readonly string SequencePalette = null;

		[Desc("Custom palette is a player palette BaseName.")]
		public readonly bool SequencePaletteIsPlayerPalette = true;

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
			: base(wr, ai, info, init)
		{
			this.info = info;
			var owner = init.Get<OwnerInit>().Value(wr.World);
			var faction = init.Get<FactionInit>().Value(wr.World);

			var rsi = ai.TraitInfo<RenderSpritesInfo>();

			if (!string.IsNullOrEmpty(info.SequencePalette))
				palette = wr.Palette(info.SequencePaletteIsPlayerPalette ? info.SequencePalette + owner.InternalName : info.SequencePalette);
			else
				palette = wr.Palette(rsi.Palette ?? rsi.PlayerPalette + owner.InternalName);

			preview = new Animation(wr.World, rsi.GetImage(ai, wr.World.Map.Rules.Sequences, faction));
			preview.PlayRepeating(info.Sequence);
		}

		protected override void TickInner()
		{
			preview.Tick();
		}

		protected override IEnumerable<IRenderable> RenderInner(WorldRenderer wr, CPos topLeft, Dictionary<CPos, PlaceBuildingCellType> footprint)
		{
			foreach (var r in RenderDecorations(wr, topLeft))
				yield return r;

			if (info.FootprintUnderPreview != PlaceBuildingCellType.None)
				foreach (var r in RenderFootprint(wr, topLeft, footprint, info.FootprintUnderPreview))
					yield return r;

			var centerPosition = wr.World.Map.CenterOfCell(topLeft) + centerOffset;
			foreach (var r in preview.Render(centerPosition, WVec.Zero, 0, palette, 1.0f))
				yield return r;

			if (info.FootprintOverPreview != PlaceBuildingCellType.None)
				foreach (var r in RenderFootprint(wr, topLeft, footprint, info.FootprintOverPreview))
					yield return r;
		}
	}
}
