#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Required for the map editor to work. Attach this to the world actor.")]
	public class EditorHighlightLayerInfo : EditorCellRegionLayerInfo
	{
		[SequenceReference("Image")]
		[Desc("Sequence to use for the highlight overlay.")]
		public readonly string HighlightSequence = "highlight";

		public override object Create(ActorInitializer init) { return new EditorHighlightLayer(init.Self, this); }
	}

	public class EditorHighlightLayer : EditorCellRegionLayer
	{
		readonly Sprite overlaySprite;

		public CellRegion OverlayRegion { get; private set; }

		public EditorHighlightLayer(Actor self, EditorHighlightLayerInfo info)
			: base(self, info)
		{
			overlaySprite = Map.SequenceProvider.GetSequence(info.Image, info.HighlightSequence).GetSprite(0);
		}

		public void SetHighlightRegion(CPos start, CPos end)
		{
			OverlayRegion = CellRegion.BoundingRegion(Map.Grid.Type, new[] { start, end });
		}

		public override void Clear()
		{
			OverlayRegion = null;
		}

		public override void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			base.RenderAfterWorld(wr, self);

			if (OverlayRegion != null)
				foreach (var c in OverlayRegion)
					new SpriteRenderable(overlaySprite, wr.World.Map.CenterOfCell(c),
						WVec.Zero, -511, palette, 1f, true).PrepareRender(wr).Render(wr);
		}
	}
}
