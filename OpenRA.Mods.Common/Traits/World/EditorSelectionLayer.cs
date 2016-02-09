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
	public class EditorSelectionLayerInfo : EditorCellRegionLayerInfo
	{
		[SequenceReference("Image")]
		[Desc("Sequence to use for the copy overlay.")]
		public readonly string CopySequence = "copy";

		[SequenceReference("Image")]
		[Desc("Sequence to use for the paste overlay.")]
		public readonly string PasteSequence = "paste";

		public override object Create(ActorInitializer init) { return new EditorSelectionLayer(init.Self, this); }
	}

	public class EditorSelectionLayer : EditorCellRegionLayer
	{
		readonly Sprite copySprite;
		readonly Sprite pasteSprite;

		public CellRegion CopyRegion { get; private set; }
		public CellRegion PasteRegion { get; private set; }

		public EditorSelectionLayer(Actor self, EditorSelectionLayerInfo info)
			: base(self, info)
		{
			copySprite = Map.SequenceProvider.GetSequence(info.Image, info.CopySequence).GetSprite(0);
			pasteSprite = Map.SequenceProvider.GetSequence(info.Image, info.PasteSequence).GetSprite(0);
		}

		public void SetCopyRegion(CPos start, CPos end)
		{
			CopyRegion = CellRegion.BoundingRegion(Map.Grid.Type, new[] { start, end });
		}

		public void SetPasteRegion(CPos start, CPos end)
		{
			PasteRegion = CellRegion.BoundingRegion(Map.Grid.Type, new[] { start, end });
		}

		public override void Clear()
		{
			CopyRegion = PasteRegion = null;
		}

		public override void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			base.RenderAfterWorld(wr, self);

			if (CopyRegion != null)
				foreach (var c in CopyRegion)
					new SpriteRenderable(copySprite, wr.World.Map.CenterOfCell(c),
						WVec.Zero, -511, palette, 1f, true).PrepareRender(wr).Render(wr);

			if (PasteRegion != null)
				foreach (var c in PasteRegion)
					new SpriteRenderable(pasteSprite, wr.World.Map.CenterOfCell(c),
						WVec.Zero, -511, palette, 1f, true).PrepareRender(wr).Render(wr);
		}
	}
}
