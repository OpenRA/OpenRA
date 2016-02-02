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
	public class EditorHighlightLayerInfo : ITraitInfo
	{
		[PaletteReference]
		[Desc("Palette to use for rendering the placement sprite.")]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		[Desc("Sequence image where the selection overlay types are defined.")]
		public readonly string Image = "editor-overlay";

		[SequenceReference("Image")]
		[Desc("Sequence to use for the highlight overlay.")]
		public readonly string HighlightSequence = "highlight";

		public virtual object Create(ActorInitializer init) { return new EditorHighlightLayer(init.Self, this); }
	}

	public class EditorHighlightLayer : IWorldLoaded, IPostRender
	{
		readonly EditorHighlightLayerInfo info;
		readonly Map map;
		readonly Sprite overlaySprite;
		PaletteReference palette;

		public CellRegion OverlayRegion { get; private set; }

		public EditorHighlightLayer(Actor self, EditorHighlightLayerInfo info)
		{
			if (self.World.Type != WorldType.Editor)
				return;

			this.info = info;
			map = self.World.Map;
			overlaySprite = map.SequenceProvider.GetSequence(info.Image, info.HighlightSequence).GetSprite(0);
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			if (w.Type != WorldType.Editor)
				return;

			palette = wr.Palette(info.Palette);
		}

		public void SetHighlightRegion(CPos start, CPos end)
		{
			OverlayRegion = CellRegion.BoundingRegion(map.Grid.Type, new[] { start, end });
		}

		public void Clear()
		{
			OverlayRegion = null;
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (wr.World.Type != WorldType.Editor)
				return;

			if (OverlayRegion != null)
				foreach (var c in OverlayRegion)
					new SpriteRenderable(overlaySprite, wr.World.Map.CenterOfCell(c),
						WVec.Zero, -511, palette, 1f, true).PrepareRender(wr).Render(wr);
		}
	}
}
