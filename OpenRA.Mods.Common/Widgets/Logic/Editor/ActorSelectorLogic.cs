#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ActorSelectorLogic : ChromeLogic
	{
		class ActorSelectorActor
		{
			public readonly ActorInfo Actor;
			public readonly string[] Categories;
			public readonly string[] SearchTerms;
			public readonly string Tooltip;

			public ActorSelectorActor(ActorInfo actor, string[] categories, string[] searchTerms, string tooltip)
			{
				Actor = actor;
				Categories = categories;
				SearchTerms = searchTerms;
				Tooltip = tooltip;
			}
		}

		readonly EditorViewportControllerWidget editor;
		readonly DropDownButtonWidget ownersDropDown;
		readonly ScrollPanelWidget panel;
		readonly ScrollItemWidget itemTemplate;
		readonly Ruleset mapRules;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly string[] allCategories;
		readonly HashSet<string> selectedCategories = new HashSet<string>();
		readonly List<string> filteredCategories = new List<string>();

		readonly ActorSelectorActor[] allActors;

		PlayerReference selectedOwner;
		string searchFilter;

		[ObjectCreator.UseCtor]
		public ActorSelectorLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			mapRules = world.Map.Rules;
			this.world = world;
			this.worldRenderer = worldRenderer;

			editor = widget.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			ownersDropDown = widget.Get<DropDownButtonWidget>("OWNERS_DROPDOWN");

			panel = widget.Get<ScrollPanelWidget>("ACTORTEMPLATE_LIST");
			itemTemplate = panel.Get<ScrollItemWidget>("ACTORPREVIEW_TEMPLATE");
			panel.Layout = new GridLayout(panel);

			var editorLayer = world.WorldActor.Trait<EditorActorLayer>();

			selectedOwner = editorLayer.Players.Players.Values.First();
			Func<PlayerReference, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
			{
				var item = ScrollItemWidget.Setup(template, () => selectedOwner == option, () =>
				{
					selectedOwner = option;

					ownersDropDown.Text = selectedOwner.Name;
					ownersDropDown.TextColor = selectedOwner.Color.RGB;

					InitializeActorPreviews();
				});

				item.Get<LabelWidget>("LABEL").GetText = () => option.Name;
				item.GetColor = () => option.Color.RGB;

				return item;
			};

			ownersDropDown.OnClick = () =>
			{
				var owners = editorLayer.Players.Players.Values.OrderBy(p => p.Name);
				ownersDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 270, owners, setupItem);
			};

			ownersDropDown.Text = selectedOwner.Name;
			ownersDropDown.TextColor = selectedOwner.Color.RGB;

			var tileSetId = world.Map.Rules.TileSet.Id;
			var allActorsTemp = new List<ActorSelectorActor>();
			foreach (var a in mapRules.Actors.Values)
			{
				// Partial templates are not allowed
				if (a.Name.Contains('^'))
					continue;

				// Actor must have a preview associated with it
				if (!a.HasTraitInfo<IRenderActorPreviewInfo>())
					continue;

				var editorData = a.TraitInfoOrDefault<EditorTilesetFilterInfo>();

				// Actor must be included in at least one category
				if (editorData == null || editorData.Categories == null)
					continue;

				// Excluded by tileset
				if (editorData.ExcludeTilesets != null && editorData.ExcludeTilesets.Contains(tileSetId))
					continue;

				if (editorData.RequireTilesets != null && !editorData.RequireTilesets.Contains(tileSetId))
					continue;

				var tooltip = a.TraitInfos<EditorOnlyTooltipInfo>().FirstOrDefault(ti => ti.EnabledByDefault) as TooltipInfoBase
					?? a.TraitInfos<TooltipInfo>().FirstOrDefault(ti => ti.EnabledByDefault);

				var searchTerms = new List<string>() { a.Name };
				if (tooltip != null)
					searchTerms.Add(tooltip.Name);

				var tooltipText = (tooltip == null ? "Type: " : tooltip.Name + "\nType: ") + a.Name;
				allActorsTemp.Add(new ActorSelectorActor(a, editorData.Categories, searchTerms.ToArray(), tooltipText));
 			}

			allActors = allActorsTemp.ToArray();

			allCategories = allActors.SelectMany(ac => ac.Categories)
				.Distinct()
				.OrderBy(x => x)
				.ToArray();

			foreach (var c in allCategories)
			{
				selectedCategories.Add(c);
				filteredCategories.Add(c);
			}

			var searchTextField = widget.Get<TextFieldWidget>("SEARCH_TEXTFIELD");
			searchTextField.OnTextEdited = () =>
			{
				searchFilter = searchTextField.Text.Trim();
				filteredCategories.Clear();

				if (!string.IsNullOrEmpty(searchFilter))
					filteredCategories.AddRange(
						allActors.Where(t => t.SearchTerms.Any(
							s => s.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
						.SelectMany(t => t.Categories)
						.Distinct()
						.OrderBy(x => x));
				else
					filteredCategories.AddRange(allCategories);

				InitializeActorPreviews();
			};

			var actorCategorySelector = widget.Get<DropDownButtonWidget>("CATEGORIES_DROPDOWN");
			actorCategorySelector.GetText = () =>
			{
				if (selectedCategories.Count == 0)
					return "None";

				if (!string.IsNullOrEmpty(searchFilter))
					return "Search Results";

				if (selectedCategories.Count == 1)
					return selectedCategories.First();

				if (selectedCategories.Count == allCategories.Length)
					return "All";

				return "Multiple";
			};

			actorCategorySelector.OnMouseDown = _ =>
			{
				if (searchTextField != null)
					searchTextField.YieldKeyboardFocus();

				actorCategorySelector.RemovePanel();
				actorCategorySelector.AttachPanel(CreateCategoriesPanel());
			};

			InitializeActorPreviews();
		}

		Widget CreateCategoriesPanel()
		{
			var categoriesPanel = Ui.LoadWidget("ACTOR_CATEGORY_FILTER_PANEL", null, new WidgetArgs());
			var categoryTemplate = categoriesPanel.Get<CheckboxWidget>("CATEGORY_TEMPLATE");

			var selectButtons = categoriesPanel.Get<ContainerWidget>("SELECT_CATEGORIES_BUTTONS");
			categoriesPanel.AddChild(selectButtons);

			var selectAll = selectButtons.Get<ButtonWidget>("SELECT_ALL");
			selectAll.OnClick = () =>
			{
				selectedCategories.Clear();
				foreach (var c in allCategories)
					selectedCategories.Add(c);

				InitializeActorPreviews();
			};

			var selectNone = selectButtons.Get<ButtonWidget>("SELECT_NONE");
			selectNone.OnClick = () =>
			{
				selectedCategories.Clear();
				InitializeActorPreviews();
			};

			var categoryHeight = 5 + selectButtons.Bounds.Height;
			foreach (var cat in filteredCategories)
			{
				var category = (CheckboxWidget)categoryTemplate.Clone();
				category.GetText = () => cat;
				category.IsChecked = () => selectedCategories.Contains(cat);
				category.IsVisible = () => true;
				category.OnClick = () =>
				{
					if (!selectedCategories.Remove(cat))
						selectedCategories.Add(cat);

					InitializeActorPreviews();
				};

				categoriesPanel.AddChild(category);
				categoryHeight += categoryTemplate.Bounds.Height;
			}

			categoriesPanel.Bounds.Height = Math.Min(categoryHeight, panel.Bounds.Height);

			return categoriesPanel;
		}

		void InitializeActorPreviews()
		{
			panel.RemoveChildren();
			if (!selectedCategories.Any())
				return;

			foreach (var a in allActors)
			{
				if (!selectedCategories.Overlaps(a.Categories))
					continue;

				if (!string.IsNullOrEmpty(searchFilter) && !a.SearchTerms.Any(s => s.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
					continue;

				var actor = a.Actor;
				var td = new TypeDictionary();
				td.Add(new OwnerInit(selectedOwner.Name));
				td.Add(new FactionInit(selectedOwner.Faction));
				foreach (var api in actor.TraitInfos<IActorPreviewInitInfo>())
					foreach (var o in api.ActorPreviewInits(actor, ActorPreviewType.MapEditorSidebar))
						td.Add(o);

				try
				{
					var item = ScrollItemWidget.Setup(itemTemplate,
						() => { var brush = editor.CurrentBrush as EditorActorBrush; return brush != null && brush.Actor == actor; },
						() => editor.SetBrush(new EditorActorBrush(editor, actor, selectedOwner, worldRenderer)));

					var preview = item.Get<ActorPreviewWidget>("ACTOR_PREVIEW");
					preview.SetPreview(actor, td);

					// Scale templates to fit within the panel
					var scale = 1f;
					if (scale * preview.IdealPreviewSize.X > itemTemplate.Bounds.Width)
						scale = (itemTemplate.Bounds.Width - panel.ItemSpacing) / (float)preview.IdealPreviewSize.X;

					preview.GetScale = () => scale;
					preview.Bounds.Width = (int)(scale * preview.IdealPreviewSize.X);
					preview.Bounds.Height = (int)(scale * preview.IdealPreviewSize.Y);

					item.Bounds.Width = preview.Bounds.Width + 2 * preview.Bounds.X;
					item.Bounds.Height = preview.Bounds.Height + 2 * preview.Bounds.Y;
					item.IsVisible = () => true;

					item.GetTooltipText = () => a.Tooltip;

					panel.AddChild(item);
				}
				catch
				{
					Log.Write("debug", "Map editor ignoring actor {0}, because of missing sprites for tileset {1}.",
						actor.Name, world.Map.Rules.TileSet.Id);
					continue;
				}
			}
		}
	}
}
