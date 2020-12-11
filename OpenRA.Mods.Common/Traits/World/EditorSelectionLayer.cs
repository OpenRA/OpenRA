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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Required for the map editor to work. Attach this to the world actor.")]
	public class EditorSelectionLayerInfo : TraitInfo
	{
		[PaletteReference]
		[Desc("Palette to use for rendering the placement sprite.")]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		[Desc("Sequence image where the selection overlay types are defined.")]
		public readonly string Image = "editor-overlay";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence to use for the copy overlay.")]
		public readonly string CopySequence = "copy";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence to use for the paste overlay.")]
		public readonly string PasteSequence = "paste";

		public override object Create(ActorInitializer init) { return new EditorSelectionLayer(init.Self, this); }
	}

	public class EditorSelectionLayer : IWorldLoaded, IRenderAboveShroud
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

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
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

		IEnumerable<IRenderable> IRenderAboveShroud.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (wr.World.Type != WorldType.Editor)
				yield break;

			if (CopyRegion != null)
				foreach (var c in CopyRegion)
					yield return new SpriteRenderable(copySprite, wr.World.Map.CenterOfCell(c),
						WVec.Zero, -511, palette, 1f, true, TintModifiers.IgnoreWorldTint);

			if (PasteRegion != null)
				foreach (var c in PasteRegion)
					yield return new SpriteRenderable(pasteSprite, wr.World.Map.CenterOfCell(c),
						WVec.Zero, -511, palette, 1f, true, TintModifiers.IgnoreWorldTint);
		}

		bool IRenderAboveShroud.SpatiallyPartitionable { get { return false; } }
	}
}
