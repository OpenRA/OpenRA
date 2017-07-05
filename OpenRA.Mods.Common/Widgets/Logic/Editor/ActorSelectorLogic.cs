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
		readonly EditorViewportControllerWidget editor;
		readonly DropDownButtonWidget ownersDropDown;
		readonly ScrollPanelWidget panel;
		readonly ScrollItemWidget itemTemplate;
		readonly Ruleset mapRules;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly List<string> allCategories;
		readonly List<string> selectedCategories = new List<string>();

		PlayerReference selectedOwner;

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

			var actorCategorySelector = widget.Get<DropDownButtonWidget>("ACTOR_CATEGORY");
			var filtersPanel = Ui.LoadWidget("ACTOR_CATEGORY_FILTER_PANEL", null, new WidgetArgs());
			var categoryTemplate = filtersPanel.Get<CheckboxWidget>("CATEGORY_TEMPLATE");
			var tileSetId = world.Map.Rules.TileSet.Id;
			allCategories = mapRules.Actors.Where(a => !a.Value.Name.Contains('^')).Select(a => a.Value.TraitInfoOrDefault<EditorTilesetFilterInfo>())
				.Where(i => i != null && i.Categories != null &&
					!(i.ExcludeTilesets != null && i.ExcludeTilesets.Contains(tileSetId)) && !(i.RequireTilesets != null && !i.RequireTilesets.Contains(tileSetId)))
				.SelectMany(i => i.Categories).Distinct().OrderBy(i => i).ToList();
			selectedCategories.AddRange(allCategories);

			var selectButtons = filtersPanel.Get<ContainerWidget>("SELECT_CATEGORIES_BUTTONS");
			filtersPanel.AddChild(selectButtons);
			filtersPanel.Bounds.Height = Math.Min(allCategories.Count * categoryTemplate.Bounds.Height + 5 + selectButtons.Bounds.Height, panel.Bounds.Height);

			var selectAll = selectButtons.Get<ButtonWidget>("SELECT_ALL");
			selectAll.OnClick = () =>
			{
				selectedCategories.Clear();
				selectedCategories.AddRange(allCategories);
				InitializeActorPreviews();
			};

			var selectNone = selectButtons.Get<ButtonWidget>("SELECT_NONE");
			selectNone.OnClick = () =>
			{
				selectedCategories.Clear();
				InitializeActorPreviews();
			};

			actorCategorySelector.OnMouseDown = _ =>
			{
				actorCategorySelector.RemovePanel();
				actorCategorySelector.AttachPanel(filtersPanel);
			};

			foreach (var cat in allCategories)
			{
				var category = (CheckboxWidget)categoryTemplate.Clone();
				category.GetText = () => cat;
				category.IsChecked = () => selectedCategories.Contains(cat);
				category.IsVisible = () => true;
				category.OnClick = () =>
				{
					if (selectedCategories.Contains(cat))
						selectedCategories.Remove(cat);
					else
						selectedCategories.Add(cat);

					InitializeActorPreviews();
				};

				filtersPanel.AddChild(category);
			}

			InitializeActorPreviews();
		}

		void InitializeActorPreviews()
		{
			panel.RemoveChildren();
			if (!selectedCategories.Any())
				return;

			var actors = mapRules.Actors.Where(a => !a.Value.Name.Contains('^'))
				.Select(a => a.Value);
			var tileSetId = world.Map.Rules.TileSet.Id;

			foreach (var a in actors)
			{
				var actor = a;
				if (actor.HasTraitInfo<BridgeInfo>()) // bridge layer takes care about that automatically
					continue;

				if (!actor.HasTraitInfo<IRenderActorPreviewInfo>())
					continue;

				var filter = actor.TraitInfoOrDefault<EditorTilesetFilterInfo>();
				if (filter == null || filter.Categories == null || !filter.Categories.Intersect(selectedCategories).Any())
					continue;

				if (filter.ExcludeTilesets != null && filter.ExcludeTilesets.Contains(tileSetId))
					continue;

				if (filter.RequireTilesets != null && !filter.RequireTilesets.Contains(tileSetId))
					continue;

				var td = new TypeDictionary();
				td.Add(new FacingInit(92));
				td.Add(new TurretFacingInit(92));
				td.Add(new HideBibPreviewInit());
				td.Add(new OwnerInit(selectedOwner.Name));
				td.Add(new FactionInit(selectedOwner.Faction));

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

					var tooltip = actor.TraitInfos<EditorOnlyTooltipInfo>().FirstOrDefault(Exts.IsTraitEnabled) as TooltipInfoBase
						?? actor.TraitInfos<TooltipInfo>().FirstOrDefault(Exts.IsTraitEnabled);

					item.GetTooltipText = () => (tooltip == null ? "Type: " : tooltip.Name + "\nType: ") + actor.Name;

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
