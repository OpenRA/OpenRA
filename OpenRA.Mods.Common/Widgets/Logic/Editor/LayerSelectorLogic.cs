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

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LayerSelectorLogic : ChromeLogic
	{
		readonly EditorViewportControllerWidget editor;
		readonly WorldRenderer worldRenderer;
		readonly EditorCursorLayer editorCursor;

		readonly ScrollPanelWidget layerTemplateList;
		readonly ScrollItemWidget layerPreviewTemplate;

		[ObjectCreator.UseCtor]
		public LayerSelectorLogic(Widget widget, WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			editor = widget.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			editorCursor = worldRenderer.World.WorldActor.Trait<EditorCursorLayer>();

			layerTemplateList = widget.Get<ScrollPanelWidget>("LAYERTEMPLATE_LIST");
			layerTemplateList.Layout = new GridLayout(layerTemplateList);
			layerPreviewTemplate = layerTemplateList.Get<ScrollItemWidget>("LAYERPREVIEW_TEMPLATE");

			IntializeLayerPreview(widget);
		}

		void IntializeLayerPreview(Widget widget)
		{
			layerTemplateList.RemoveChildren();
			var rules = worldRenderer.World.Map.Rules;
			var resources = rules.Actors["world"].TraitInfos<ResourceTypeInfo>();
			var tileSize = worldRenderer.World.Map.Grid.TileSize;
			foreach (var resource in resources)
			{
				var newResourcePreviewTemplate = ScrollItemWidget.Setup(layerPreviewTemplate,
					() => editorCursor.Type == EditorCursorType.Resource && editorCursor.Resource == resource,
					() => editor.SetBrush(new EditorResourceBrush(editor, resource, worldRenderer)));

				newResourcePreviewTemplate.Bounds.X = 0;
				newResourcePreviewTemplate.Bounds.Y = 0;

				var layerPreview = newResourcePreviewTemplate.Get<SpriteWidget>("LAYER_PREVIEW");
				layerPreview.IsVisible = () => true;
				layerPreview.GetPalette = () => resource.Palette;

				var variant = resource.Sequences.FirstOrDefault();
				var sequence = rules.Sequences.GetSequence("resources", variant);
				var frame = sequence.Frames != null ? sequence.Frames.Last() : resource.MaxDensity - 1;
				layerPreview.GetSprite = () => sequence.GetSprite(frame);

				layerPreview.Bounds.Width = tileSize.Width;
				layerPreview.Bounds.Height = tileSize.Height;
				newResourcePreviewTemplate.Bounds.Width = tileSize.Width + (layerPreview.Bounds.X * 2);
				newResourcePreviewTemplate.Bounds.Height = tileSize.Height + (layerPreview.Bounds.Y * 2);

				newResourcePreviewTemplate.IsVisible = () => true;
				newResourcePreviewTemplate.GetTooltipText = () => resource.Type;

				layerTemplateList.AddChild(newResourcePreviewTemplate);
			}
		}
	}
}
