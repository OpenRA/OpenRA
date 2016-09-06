#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Required for the map editor to work. Attach this to the world actor.")]
	public class EditorSelectionLayerInfo : ITraitInfo
	{
		[PaletteReference]
		[Desc("Palette to use for rendering the placement sprite.")]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		[Desc("Sequence image where the selection overlay types are defined.")]
		public readonly string Image = "editor-overlay";

		[SequenceReference("Image")]
		[Desc("Sequence to use for the copy overlay.")]
		public readonly string CopySequence = "copy";

		[SequenceReference("Image")]
		[Desc("Sequence to use for the paste overlay.")]
		public readonly string PasteSequence = "paste";

		public virtual object Create(ActorInitializer init) { return new EditorSelectionLayer(init.Self, this); }
	}

	public class EditorSelectionLayer : IWorldLoaded, IPostRender
	{
		readonly EditorSelectionLayerInfo info;
		readonly Map map;
		readonly Sprite copySprite;
		readonly Sprite pasteSprite;
		PaletteReference palette;

		public CellRegion CopyRegion { get; private set; }
		public CellRegion PasteRegion { get; private set; }

		public EditorSelectionLayer(Actor self, EditorSelectionLayerInfo info)
		{
			if (self.World.Type != WorldType.Editor)
				return;

			this.info = info;
			map = self.World.Map;
			copySprite = map.Rules.Sequences.GetSequence(info.Image, info.CopySequence).GetSprite(0);
			pasteSprite = map.Rules.Sequences.GetSequence(info.Image, info.PasteSequence).GetSprite(0);
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			if (w.Type != WorldType.Editor)
				return;

			palette = wr.Palette(info.Palette);
		}

		public void SetCopyRegion(CPos start, CPos end)
		{
			CopyRegion = CellRegion.BoundingRegion(map.Grid.Type, new[] { start, end });
		}

		public void SetPasteRegion(CPos start, CPos end)
		{
			PasteRegion = CellRegion.BoundingRegion(map.Grid.Type, new[] { start, end });
		}

		public void Clear()
		{
			CopyRegion = PasteRegion = null;
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (wr.World.Type != WorldType.Editor)
				return;

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
