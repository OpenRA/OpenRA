#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LayerSelectorLogic : ChromeLogic
	{
		readonly EditorViewportControllerWidget editor;
		readonly Ruleset modRules;
		readonly World world;
		readonly WorldRenderer worldRenderer;

		readonly ScrollPanelWidget layerTemplateList;
		readonly ScrollItemWidget layerPreviewTemplate;

		[ObjectCreator.UseCtor]
		public LayerSelectorLogic(Widget widget, WorldRenderer worldRenderer, Ruleset modRules)
		{
			this.modRules = modRules;
			this.worldRenderer = worldRenderer;
			world = worldRenderer.World;

			editor = widget.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");

			layerTemplateList = widget.Get<ScrollPanelWidget>("LAYERTEMPLATE_LIST");
			layerTemplateList.Layout = new GridLayout(layerTemplateList);
			layerPreviewTemplate = layerTemplateList.Get<ScrollItemWidget>("LAYERPREVIEW_TEMPLATE");

			IntializeLayerPreview(widget);
		}

		void IntializeLayerPreview(Widget widget)
		{
			layerTemplateList.RemoveChildren();

			var resources = modRules.Actors["world"].TraitInfos<ResourceTypeInfo>();
			foreach (var resource in resources)
			{
				var newResourcePreviewTemplate = ScrollItemWidget.Setup(layerPreviewTemplate,
					() => { var brush = editor.CurrentBrush as EditorResourceBrush; return brush != null && brush.ResourceType == resource; },
					() => editor.SetBrush(new EditorResourceBrush(editor, resource, worldRenderer)));

				newResourcePreviewTemplate.Bounds.X = 0;
				newResourcePreviewTemplate.Bounds.Y = 0;

				var layerPreview = newResourcePreviewTemplate.Get<SpriteWidget>("LAYER_PREVIEW");
				layerPreview.IsVisible = () => true;
				layerPreview.GetPalette = () => resource.Palette;

				var variant = resource.Variants.FirstOrDefault();
				var sequenceProvider = modRules.Sequences[world.TileSet.Id];
				var sequence = sequenceProvider.GetSequence("resources", variant);
				var frame = sequence.Frames != null ? sequence.Frames.Last() : resource.MaxDensity - 1;
				layerPreview.GetSprite = () => sequence.GetSprite(frame);

				var tileSize = Game.ModData.Manifest.Get<MapGrid>().TileSize;
				layerPreview.Bounds.Width = tileSize.Width;
				layerPreview.Bounds.Height = tileSize.Height;
				newResourcePreviewTemplate.Bounds.Width = tileSize.Width + (layerPreview.Bounds.X * 2);
				newResourcePreviewTemplate.Bounds.Height = tileSize.Height + (layerPreview.Bounds.Y * 2);

				newResourcePreviewTemplate.IsVisible = () => true;
				newResourcePreviewTemplate.GetTooltipText = () => resource.Name;

				layerTemplateList.AddChild(newResourcePreviewTemplate);
			}
		}
	}
}
