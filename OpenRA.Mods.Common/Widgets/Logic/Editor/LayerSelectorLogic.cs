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

			IntializeLayerPreview();
		}

		void IntializeLayerPreview()
		{
			layerTemplateList.RemoveChildren();
			foreach (var resourceRenderer in worldRenderer.World.WorldActor.TraitsImplementing<IResourceRenderer>())
			{
				foreach (var resourceType in resourceRenderer.ResourceTypes)
				{
					var newResourcePreviewTemplate = ScrollItemWidget.Setup(layerPreviewTemplate,
						() => editorCursor.Type == EditorCursorType.Resource && editorCursor.ResourceType == resourceType,
						() => editor.SetBrush(new EditorResourceBrush(editor, resourceType, worldRenderer)));

					newResourcePreviewTemplate.Bounds.X = 0;
					newResourcePreviewTemplate.Bounds.Y = 0;

					var layerPreview = newResourcePreviewTemplate.Get<ResourcePreviewWidget>("LAYER_PREVIEW");
					var size = layerPreview.IdealPreviewSize;
					layerPreview.IsVisible = () => true;
					layerPreview.ResourceType = resourceType;
					layerPreview.Bounds.Width = size.Width;
					layerPreview.Bounds.Height = size.Height;
					newResourcePreviewTemplate.Bounds.Width = size.Width + (layerPreview.Bounds.X * 2);
					newResourcePreviewTemplate.Bounds.Height = size.Height + (layerPreview.Bounds.Y * 2);
					newResourcePreviewTemplate.IsVisible = () => true;
					newResourcePreviewTemplate.GetTooltipText = () => resourceType;

					layerTemplateList.AddChild(newResourcePreviewTemplate);
				}
			}
		}
	}
}
