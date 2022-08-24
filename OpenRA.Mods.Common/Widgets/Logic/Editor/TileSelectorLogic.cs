#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
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

		readonly ITemplatedTerrainInfo terrainInfo;
		readonly ITiledTerrainRenderer terrainRenderer;
		readonly TileSelectorTemplate[] allTemplates;
		readonly EditorCursorLayer editorCursor;

		[ObjectCreator.UseCtor]
		public TileSelectorLogic(Widget widget, ModData modData, World world, WorldRenderer worldRenderer)
			: base(widget, modData, world, worldRenderer, "TILETEMPLATE_LIST", "TILEPREVIEW_TEMPLATE")
		{
			terrainInfo = world.Map.Rules.TerrainInfo as ITemplatedTerrainInfo;
			if (terrainInfo == null)
				throw new InvalidDataException("TileSelectorLogic requires a template-based tileset.");

			terrainRenderer = world.WorldActor.TraitOrDefault<ITiledTerrainRenderer>();
			if (terrainRenderer == null)
				throw new InvalidDataException("TileSelectorLogic requires a tile-based terrain renderer.");

			allTemplates = terrainInfo.Templates.Values.Select(t => new TileSelectorTemplate(t)).ToArray();
			editorCursor = world.WorldActor.Trait<EditorCursorLayer>();

			allCategories = allTemplates.SelectMany(t => t.Categories)
				.Distinct()
				.OrderBy(CategoryOrder)
				.ToArray();

			foreach (var c in allCategories)
			{
				SelectedCategories.Add(c);
				FilteredCategories.Add(c);
			}

			SearchTextField.OnTextEdited = () =>
			{
				searchFilter = SearchTextField.Text.Trim();
				FilteredCategories.Clear();

				if (!string.IsNullOrEmpty(searchFilter))
					FilteredCategories.AddRange(
						allTemplates.Where(t => t.SearchTerms.Any(
							s => s.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
						.SelectMany(t => t.Categories)
						.Distinct()
						.OrderBy(CategoryOrder));
				else
					FilteredCategories.AddRange(allCategories);

				InitializePreviews();
			};

			InitializePreviews();
		}

		int CategoryOrder(string category)
		{
			var i = terrainInfo.EditorTemplateOrder.IndexOf(category);
			return i >= 0 ? i : int.MaxValue;
		}

		protected override void InitializePreviews()
		{
			Panel.RemoveChildren();
			if (SelectedCategories.Count == 0)
				return;

			foreach (var t in allTemplates)
			{
				if (!SelectedCategories.Overlaps(t.Categories))
					continue;

				if (!string.IsNullOrEmpty(searchFilter) && !t.SearchTerms.Any(s => s.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
					continue;

				var tileId = t.Template.Id;
				var item = ScrollItemWidget.Setup(ItemTemplate,
					() => editorCursor.Type == EditorCursorType.TerrainTemplate && editorCursor.TerrainTemplate.Id == tileId,
					() => Editor.SetBrush(new EditorTileBrush(Editor, tileId, WorldRenderer)));

				var preview = item.Get<TerrainTemplatePreviewWidget>("TILE_PREVIEW");
				var template = terrainInfo.Templates[tileId];
				var bounds = terrainRenderer.TemplateBounds(template);

				// Scale templates to fit within the panel
				var scale = 1f;
				while (scale * bounds.Width > ItemTemplate.Bounds.Width)
					scale /= 2;

				preview.Template = template;
				preview.GetScale = () => scale;
				preview.Bounds.Width = (int)(scale * bounds.Width);
				preview.Bounds.Height = (int)(scale * bounds.Height);

				item.Bounds.Width = preview.Bounds.Width + 2 * preview.Bounds.X;
				item.Bounds.Height = preview.Bounds.Height + 2 * preview.Bounds.Y;
				item.IsVisible = () => true;
				item.GetTooltipText = () => t.Tooltip;

				Panel.AddChild(item);
			}
		}
	}
}
