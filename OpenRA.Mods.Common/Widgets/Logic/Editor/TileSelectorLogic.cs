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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[IncludeStaticFluentReferences(
		typeof(PaintTileEditorAction),
		typeof(FloodFillEditorAction),
		typeof(CommonSelectorLogic))]
	public class TileSelectorLogic : CommonSelectorLogic
	{
		sealed class TileSelectorTemplate
		{
			public readonly TerrainTemplateInfo Template;
			public readonly ImmutableArray<string> Categories;
			public readonly string DisplayName;
			public readonly string[] SearchTerms;
			public readonly string Tooltip;

			public TileSelectorTemplate(ITemplatedTerrainInfo terrainInfo, TerrainTemplateInfo template)
			{
				Template = template;
				Categories = template.Categories;

				var id = template.Id.ToString(NumberFormatInfo.CurrentInfo);
				var images = template is DefaultTerrainTemplateInfo defaultTemplate
					? defaultTemplate.Images
					: [];
				DisplayName = images.Length > 0 ? images[0] : id;

				var terrainTypes = TerrainTypes(terrainInfo, template);
				var terms = new List<string> { id };
				terms.AddRange(images);
				terms.AddRange(Categories);
				terms.AddRange(terrainTypes);

				foreach (var image in images)
					terms.AddRange(ImageAliases(image));

				SearchTerms = terms
					.Where(t => !string.IsNullOrWhiteSpace(t))
					.Distinct(StringComparer.CurrentCultureIgnoreCase)
					.ToArray();

				var details = new[]
				{
					DisplayName,
					$"Template ID: {id}",
					Categories.Length > 0 ? $"Category: {string.Join(", ", Categories)}" : null,
					terrainTypes.Length > 0 ? $"Terrain: {string.Join(", ", terrainTypes)}" : null,
				};

				Tooltip = string.Join("\n", details.Where(d => d != null));
			}

			static ImmutableArray<string> TerrainTypes(ITemplatedTerrainInfo terrainInfo, TerrainTemplateInfo template)
			{
				var terrainTypes = new HashSet<string>();
				for (var i = 0; i < template.TilesCount; i++)
				{
					if (!template.Contains(i) || template[i] == null || template[i].TerrainType == byte.MaxValue)
						continue;

					terrainTypes.Add(terrainInfo.TerrainTypes[template[i].TerrainType].Type);
				}

				return [.. terrainTypes.Order()];
			}

			static IEnumerable<string> ImageAliases(string image)
			{
				var name = Path.GetFileNameWithoutExtension(image);
				if (string.IsNullOrEmpty(name))
					yield break;

				yield return name;

				// Original RA shore tile assets use sh## filenames.
				if (name.StartsWith("sh", StringComparison.OrdinalIgnoreCase))
					yield return "shore";
			}
		}

		readonly ITemplatedTerrainInfo terrainInfo;
		readonly ImmutableArray<TileSelectorTemplate> allTemplates;

		[ObjectCreator.UseCtor]
		public TileSelectorLogic(Widget widget, ModData modData, World world, WorldRenderer worldRenderer)
			: base(widget, modData, world, worldRenderer, "TILETEMPLATE_LIST", "TILEPREVIEW_TEMPLATE")
		{
			terrainInfo = world.Map.Rules.TerrainInfo as ITemplatedTerrainInfo;
			if (terrainInfo == null)
				throw new InvalidDataException("TileSelectorLogic requires a template-based tileset.");

			allTemplates = terrainInfo.TemplatesInDefinitionOrder.Select(t => new TileSelectorTemplate(terrainInfo, t)).ToImmutableArray();

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
							s => s.Contains(searchFilter, StringComparison.CurrentCultureIgnoreCase)))
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

				if (!string.IsNullOrEmpty(searchFilter) &&
					!t.SearchTerms.Any(s => s.Contains(searchFilter, StringComparison.CurrentCultureIgnoreCase)))
					continue;

				var tileId = t.Template.Id;
				var item = ScrollItemWidget.Setup(ItemTemplate,
					() => Editor.CurrentBrush is EditorTileBrush editorCursor && editorCursor.TerrainTemplate.Id == tileId,
					() => Editor.SetBrush(new EditorTileBrush(Editor, tileId, WorldRenderer)));

				var preview = item.Get<TerrainTemplatePreviewWidget>("TILE_PREVIEW");
				preview.SetTemplate(terrainInfo.Templates[tileId]);
				var label = item.Get<LabelWidget>("TILE_NAME");
				label.GetText = () => t.DisplayName;

				// Scale templates to fit within the panel
				// Preview position is assumed to be a margin
				var maxPreviewWidth = item.Bounds.Width - 2 * preview.Bounds.X;
				var maxPreviewHeight = item.Bounds.Height - 2 * preview.Bounds.Y - label.Bounds.Height;

				var scale = 1f;
				if (preview.IdealPreviewSize.X > maxPreviewWidth)
					scale = maxPreviewWidth / (float)preview.IdealPreviewSize.X;

				if (preview.IdealPreviewSize.Y * scale > maxPreviewHeight)
					scale = maxPreviewHeight / (float)preview.IdealPreviewSize.Y;

				preview.Scale = scale;
				preview.Bounds.Width = (int)(scale * preview.IdealPreviewSize.X);
				preview.Bounds.Height = (int)(scale * preview.IdealPreviewSize.Y);

				item.Bounds.Width = preview.Bounds.Width + 2 * preview.Bounds.X;
				item.Bounds.Height = preview.Bounds.Height + 2 * preview.Bounds.Y + label.Bounds.Height;
				label.Bounds.Y = item.Bounds.Height - label.Bounds.Height;
				label.Bounds.Width = item.Bounds.Width - 2 * label.Bounds.X;
				item.IsVisible = () => true;
				item.GetTooltipText = () => t.Tooltip;

				Panel.AddChild(item);
			}
		}
	}
}
