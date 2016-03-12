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

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class TileSelectorLogic : ChromeLogic
	{
		readonly EditorViewportControllerWidget editor;
		readonly ScrollPanelWidget panel;
		readonly ScrollItemWidget itemTemplate;

		[ObjectCreator.UseCtor]
		public TileSelectorLogic(Widget widget, WorldRenderer worldRenderer)
		{
			var rules = worldRenderer.World.Map.Rules;
			var tileset = rules.TileSet;

			editor = widget.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			panel = widget.Get<ScrollPanelWidget>("TILETEMPLATE_LIST");
			itemTemplate = panel.Get<ScrollItemWidget>("TILEPREVIEW_TEMPLATE");
			panel.Layout = new GridLayout(panel);

			var tileCategorySelector = widget.Get<DropDownButtonWidget>("TILE_CATEGORY");
			var categories = tileset.EditorTemplateOrder;
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
			{
				var item = ScrollItemWidget.Setup(template,
					() => tileCategorySelector.Text == option,
					() => { tileCategorySelector.Text = option; IntializeTilePreview(widget, worldRenderer, tileset, option); });

				item.Get<LabelWidget>("LABEL").GetText = () => option;
				return item;
			};

			tileCategorySelector.OnClick = () =>
				tileCategorySelector.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 270, categories, setupItem);

			tileCategorySelector.Text = categories.First();
			IntializeTilePreview(widget, worldRenderer, tileset, categories.First());
		}

		void IntializeTilePreview(Widget widget, WorldRenderer worldRenderer, TileSet tileset, string category)
		{
			panel.RemoveChildren();

			var tileIds = tileset.Templates
				.Where(t => t.Value.Category == category)
				.Select(t => t.Value.Id);

			foreach (var t in tileIds)
			{
				var tileId = t;
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => { var brush = editor.CurrentBrush as EditorTileBrush; return brush != null && brush.Template == tileId; },
					() => editor.SetBrush(new EditorTileBrush(editor, tileId, worldRenderer)));

				var preview = item.Get<TerrainTemplatePreviewWidget>("TILE_PREVIEW");
				var template = tileset.Templates[tileId];
				var grid = worldRenderer.World.Map.Grid;
				var bounds = worldRenderer.Theater.TemplateBounds(template, grid.TileSize, grid.Type);

				// Scale templates to fit within the panel
				var scale = 1f;
				while (scale * bounds.Width > itemTemplate.Bounds.Width)
					scale /= 2;

				preview.Template = template;
				preview.GetScale = () => scale;
				preview.Bounds.Width = (int)(scale * bounds.Width);
				preview.Bounds.Height = (int)(scale * bounds.Height);

				item.Bounds.Width = preview.Bounds.Width + 2 * preview.Bounds.X;
				item.Bounds.Height = preview.Bounds.Height + 2 * preview.Bounds.Y;
				item.IsVisible = () => true;
				item.GetTooltipText = () => tileId.ToString();

				panel.AddChild(item);
			}
		}
	}
}
