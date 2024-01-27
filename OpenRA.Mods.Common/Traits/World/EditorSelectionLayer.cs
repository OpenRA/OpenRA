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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;
using Color = OpenRA.Primitives.Color;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	[Desc("Renders the selection grid in the editor.")]
	public class EditorSelectionLayerInfo : TraitInfo, Requires<LoadWidgetAtGameStartInfo>, IEditorSelectionLayer
	{
		[Desc("Main color of the selection grid.")]
		public readonly Color MainColor = Color.White;

		[Desc("Alternate color of the selection grid.")]
		public readonly Color AltColor = Color.Black;

		[Desc("Main color of the paste grid.")]
		public readonly Color PasteColor = Color.FromArgb(0xFF4CFF00);

		[Desc("Thickness of the selection grid lines.")]
		public readonly int LineThickness = 1;

		[Desc("Render offset of the secondary grid lines.")]
		public readonly int2 AltPixelOffset = new(1, 1);

		public override object Create(ActorInitializer init) { return new EditorSelectionLayer(this); }
	}

	public class EditorSelectionLayer : IRenderAnnotations, IWorldLoaded
	{
		readonly EditorSelectionLayerInfo info;
		EditorViewportControllerWidget editor;

		public EditorSelectionLayer(EditorSelectionLayerInfo info)
		{
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			var worldRoot = Ui.Root.Get<ContainerWidget>("EDITOR_WORLD_ROOT");
			editor = worldRoot.Get<EditorViewportControllerWidget>("MAP_EDITOR");
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (editor.DefaultBrush.CurrentDragBounds != null)
			{
				yield return new EditorSelectionAnnotationRenderable(editor.DefaultBrush.CurrentDragBounds, info.AltColor, info.AltPixelOffset, null);
				yield return new EditorSelectionAnnotationRenderable(editor.DefaultBrush.CurrentDragBounds, info.MainColor, int2.Zero, null);
			}

			if (editor.CurrentBrush is EditorCopyPasteBrush pasteBrush && pasteBrush.PastePreviewPosition != null)
			{
				yield return new EditorSelectionAnnotationRenderable(pasteBrush.Region, info.AltColor, info.AltPixelOffset, pasteBrush.PastePreviewPosition);
				yield return new EditorSelectionAnnotationRenderable(pasteBrush.Region, info.PasteColor, int2.Zero, pasteBrush.PastePreviewPosition);
			}
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
