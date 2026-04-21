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
	[IncludeStaticFluentReferences(typeof(AddResourcesEditorAction))]
	public class LayerSelectorLogic : ChromeLogic
	{
		readonly EditorViewportControllerWidget editor;
		readonly WorldRenderer worldRenderer;

		readonly ScrollPanelWidget layerTemplateList;
		readonly ScrollItemWidget layerPreviewTemplate;

		[ObjectCreator.UseCtor]
		public LayerSelectorLogic(Widget widget, WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			editor = widget.Parent.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
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
					var item = ScrollItemWidget.Setup(layerPreviewTemplate,
						() => editor.CurrentBrush is EditorResourceBrush brush && brush.ResourceType == resourceType,
						() => editor.SetBrush(new EditorResourceBrush(editor, resourceType, worldRenderer)));

					var preview = item.Get<ResourcePreviewWidget>("LAYER_PREVIEW");
					preview.SetResourceType(resourceType);

					// Scale templates to fit within the panel
					// Preview position is assumed to be a margin
					var maxPreviewWidth = item.Bounds.Width - 2 * preview.Bounds.X;
					var maxPreviewHeight = item.Bounds.Height - 2 * preview.Bounds.Y;

					var scale = 1f;
					if (preview.IdealPreviewSize.Width > maxPreviewWidth)
						scale = maxPreviewWidth / (float)preview.IdealPreviewSize.Width;

					if (preview.IdealPreviewSize.Height * scale > maxPreviewHeight)
						scale = maxPreviewHeight / (float)preview.IdealPreviewSize.Height;

					preview.Scale = scale;
					preview.Bounds.Width = (int)(scale * preview.IdealPreviewSize.Width);
					preview.Bounds.Height = (int)(scale * preview.IdealPreviewSize.Height);

					item.Bounds.Width = preview.Bounds.Width + 2 * preview.Bounds.X;
					item.Bounds.Height = preview.Bounds.Height + 2 * preview.Bounds.Y;
					item.IsVisible = () => true;
					item.GetTooltipText = () => resourceType;

					layerTemplateList.AddChild(item);
				}
			}
		}
	}
}
