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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ActorSelectorLogic : CommonSelectorLogic
	{
		[TranslationReference("actorType")]
		const string ActorTypeTooltip = "label-actor-type";

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

		readonly DropDownButtonWidget ownersDropDown;
		readonly Ruleset mapRules;
		readonly ActorSelectorActor[] allActors;
		readonly EditorCursorLayer editorCursor;

		PlayerReference selectedOwner;

		[ObjectCreator.UseCtor]
		public ActorSelectorLogic(Widget widget, ModData modData, World world, WorldRenderer worldRenderer)
			: base(widget, modData, world, worldRenderer, "ACTORTEMPLATE_LIST", "ACTORPREVIEW_TEMPLATE")
		{
			mapRules = world.Map.Rules;
			ownersDropDown = widget.Get<DropDownButtonWidget>("OWNERS_DROPDOWN");
			editorCursor = world.WorldActor.Trait<EditorCursorLayer>();
			var editorLayer = world.WorldActor.Trait<EditorActorLayer>();

			selectedOwner = editorLayer.Players.Players.Values.First();
			Func<PlayerReference, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
			{
				var item = ScrollItemWidget.Setup(template, () => selectedOwner == option, () => SelectOwner(option));

				item.Get<LabelWidget>("LABEL").GetText = () => option.Name;
				item.GetColor = () => option.Color;

				return item;
			};

			editorLayer.OnPlayerRemoved = () =>
			{
				if (editorLayer.Players.Players.Values.Any(p => p.Name == selectedOwner.Name))
					return;
				SelectOwner(editorLayer.Players.Players.Values.First());
			};

			ownersDropDown.OnClick = () =>
			{
				var owners = editorLayer.Players.Players.Values.OrderBy(p => p.Name);
				ownersDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 270, owners, setupItem);
			};

			ownersDropDown.Text = selectedOwner.Name;
			ownersDropDown.TextColor = selectedOwner.Color;

			var tileSetId = world.Map.Rules.TerrainInfo.Id;
			var allActorsTemp = new List<ActorSelectorActor>();
			foreach (var a in mapRules.Actors.Values)
			{
				// Partial templates are not allowed
				if (a.Name.Contains('^'))
					continue;

				// Actor must have a preview associated with it
				if (!a.HasTraitInfo<IRenderActorPreviewInfo>())
					continue;

				var editorData = a.TraitInfoOrDefault<MapEditorDataInfo>();

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

				var actorType = modData.Translation.GetString(ActorTypeTooltip, Translation.Arguments("actorType", a.Name));
				var tooltipText = tooltip == null ? actorType : tooltip.Name + $"\n{actorType}";
				allActorsTemp.Add(new ActorSelectorActor(a, editorData.Categories, searchTerms.ToArray(), tooltipText));
			}

			allActors = allActorsTemp.ToArray();

			allCategories = allActors.SelectMany(ac => ac.Categories)
				.Distinct()
				.OrderBy(x => x)
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
						allActors.Where(t => t.SearchTerms.Any(
							s => s.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
						.SelectMany(t => t.Categories)
						.Distinct()
						.OrderBy(x => x));
				else
					FilteredCategories.AddRange(allCategories);

				InitializePreviews();
			};

			InitializePreviews();
		}

		void SelectOwner(PlayerReference option)
		{
			selectedOwner = option;
			ownersDropDown.Text = option.Name;
			ownersDropDown.TextColor = option.Color;
			InitializePreviews();

			var actor = editorCursor.Actor;
			if (actor != null)
			{
				actor.Owner = option;
				actor.ReplaceInit(new OwnerInit(option.Name));
				actor.ReplaceInit(new FactionInit(option.Faction));
			}
		}

		protected override void InitializePreviews()
		{
			Panel.RemoveChildren();
			if (SelectedCategories.Count == 0)
				return;

			foreach (var a in allActors)
			{
				if (!SelectedCategories.Overlaps(a.Categories))
					continue;

				if (!string.IsNullOrEmpty(searchFilter) && !a.SearchTerms.Any(s => s.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0))
					continue;

				var actor = a.Actor;
				var td = new TypeDictionary
				{
					new OwnerInit(selectedOwner.Name),
					new FactionInit(selectedOwner.Faction)
				};
				foreach (var api in actor.TraitInfos<IActorPreviewInitInfo>())
					foreach (var o in api.ActorPreviewInits(actor, ActorPreviewType.MapEditorSidebar))
						td.Add(o);

				try
				{
					var item = ScrollItemWidget.Setup(ItemTemplate,
						() => editorCursor.Type == EditorCursorType.Actor && editorCursor.Actor.Info == actor,
						() => Editor.SetBrush(new EditorActorBrush(Editor, actor, selectedOwner, WorldRenderer)));

					var preview = item.Get<ActorPreviewWidget>("ACTOR_PREVIEW");
					preview.SetPreview(actor, td);

					// Scale templates to fit within the panel
					var scale = 1f;
					if (scale * preview.IdealPreviewSize.X > ItemTemplate.Bounds.Width)
						scale = (ItemTemplate.Bounds.Width - Panel.ItemSpacing) / (float)preview.IdealPreviewSize.X;

					preview.GetScale = () => scale;
					preview.Bounds.Width = (int)(scale * preview.IdealPreviewSize.X);
					preview.Bounds.Height = (int)(scale * preview.IdealPreviewSize.Y);

					item.Bounds.Width = preview.Bounds.Width + 2 * preview.Bounds.X;
					item.Bounds.Height = preview.Bounds.Height + 2 * preview.Bounds.Y;
					item.IsVisible = () => true;

					item.GetTooltipText = () => a.Tooltip;

					Panel.AddChild(item);
				}
				catch
				{
					Log.Write("debug", "Map editor ignoring actor {0}, because of missing sprites for tileset {1}.",
						actor.Name, World.Map.Rules.TerrainInfo.Id);
					continue;
				}
			}
		}
	}
}
