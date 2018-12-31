#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	public class TileSelectorLogic : CommonSelectorLogic
	{
		class TileSelectorTemplate
		{
			public readonly TerrainTemplateInfo Template;
			public readonly string[] Categories;
			public readonly string[] SearchTerms;
			public readonly string Tooltip;

			public TileSelectorTemplate(TerrainTemplateInfo template)
			{
				Template = template;
				Categories = template.Categories;
				Tooltip = template.Id.ToString();
				SearchTerms = new[] { Tooltip };
			}
		}

		readonly TileSet tileset;
		readonly ScrollPanelWidget panel;
		readonly ScrollItemWidget itemTemplate;
		readonly TileSelectorTemplate[] allTemplates;

		string selectedCategory;
		string userSelectedCategory;

		[ObjectCreator.UseCtor]
		public TileSelectorLogic(Widget widget, World world, WorldRenderer worldRenderer) :
			base(widget, world, worldRenderer)
		{
			tileset = world.Map.Rules.TileSet;

			panel = widget.Get<ScrollPanelWidget>("TILETEMPLATE_LIST");
			itemTemplate = panel.Get<ScrollItemWidget>("TILEPREVIEW_TEMPLATE");
			panel.Layout = new GridLayout(panel);

			allTemplates = tileset.Templates.Values.Select(t => new TileSelectorTemplate(t)).ToArray();

			var orderedCategories = allTemplates.SelectMany(t => t.Categories)
				.Distinct()
				.OrderBy(CategoryOrder)
				.ToArray();

			var searchTextField = widget.Get<TextFieldWidget>("SEARCH_TEXTFIELD");
			searchTextField.OnTextEdited = () =>
			{
				searchFilter = searchTextField.Text.Trim();
				selectedCategory = string.IsNullOrEmpty(searchFilter) ? userSelectedCategory : null;

				InitializePreviews();
			};

			searchTextField.OnEscKey = () =>
			{
				searchTextField.Text = "";
				searchTextField.YieldKeyboardFocus();
				return true;
			};

			Func<string, string> categoryTitle = s => s != null ? s : "Search Results";
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
			{
				var item = ScrollItemWidget.Setup(template,	() => selectedCategory == option, () =>
				{
					selectedCategory = option;
					if (option != null)
						userSelectedCategory = option;

					InitializePreviews();
				});

				var title = categoryTitle(option);
				item.Get<LabelWidget>("LABEL").GetText = () => title;
				return item;
			};

			var tileCategorySelector = widget.Get<DropDownButtonWidget>("CATEGORIES_DROPDOWN");
			tileCategorySelector.OnClick = () =>
			{
				if (searchTextField != null)
					searchTextField.YieldKeyboardFocus();

				var categories = orderedCategories.AsEnumerable();
				if (!string.IsNullOrEmpty(searchFilter))
				{
					var filteredCategories = allTemplates.Where(t => t.SearchTerms.Any(
							s => s.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
						.SelectMany(t => t.Categories)
						.Distinct()
						.OrderBy(CategoryOrder);
					categories = new string[] { null }.Concat(filteredCategories);
				}

				tileCategorySelector.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 270, categories, setupItem);
			};

			var actorCategorySelector = widget.Get<DropDownButtonWidget>("CATEGORIES_DROPDOWN");
			actorCategorySelector.GetText = () => categoryTitle(selectedCategory);

			selectedCategory = userSelectedCategory = orderedCategories.First();
			InitializePreviews();
		}

		int CategoryOrder(string category)
		{
			var i = tileset.EditorTemplateOrder.IndexOf(category);
			return i >= 0 ? i : int.MaxValue;
		}

		protected override void InitializePreviews()
		{
			panel.RemoveChildren();

			foreach (var t in allTemplates)
			{
				if (selectedCategory != null && !t.Categories.Contains(selectedCategory))
					continue;

				if (!string.IsNullOrEmpty(searchFilter) && !t.SearchTerms.Any(s => s.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
					continue;

				var tileId = t.Template.Id;
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => { var brush = Editor.CurrentBrush as EditorTileBrush; return brush != null && brush.Template == tileId; },
					() => Editor.SetBrush(new EditorTileBrush(Editor, tileId, WorldRenderer)));

				var preview = item.Get<TerrainTemplatePreviewWidget>("TILE_PREVIEW");
				var template = tileset.Templates[tileId];
				var grid = WorldRenderer.World.Map.Grid;
				var bounds = WorldRenderer.Theater.TemplateBounds(template, grid.TileSize, grid.Type);

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
				item.GetTooltipText = () => t.Tooltip;

				panel.AddChild(item);
			}
		}
	}
}
