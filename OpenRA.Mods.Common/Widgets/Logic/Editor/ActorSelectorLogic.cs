#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ActorSelectorLogic
	{
		readonly MapEditorWidget editor;
		readonly ScrollPanelWidget panel;
		readonly ScrollItemWidget itemTemplate;
		readonly Ruleset modRules;

		[ObjectCreator.UseCtor]
		public ActorSelectorLogic(Widget widget, World world, WorldRenderer worldRenderer, Ruleset modRules)
		{
			this.modRules = modRules;

			editor = widget.Parent.Get<MapEditorWidget>("MAP_EDITOR");

			panel = widget.Get<ScrollPanelWidget>("ACTORTEMPLATE_LIST");
			itemTemplate = panel.Get<ScrollItemWidget>("ACTORPREVIEW_TEMPLATE");
			panel.Layout = new GridLayout(panel);

			var ownersDropDown = widget.Get<DropDownButtonWidget>("OWNERS_DROPDOWN");
			var ownernames = world.Map.Players.Values.Select(p => p.Name);
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
			{
				var player = world.Players.FirstOrDefault(p => p.InternalName == option) ?? world.Players.First();
				var item = ScrollItemWidget.Setup(template,
					() => ownersDropDown.Text == option,
					() => { ownersDropDown.TextColor = player.Color.RGB;
						ownersDropDown.Text = option; editor.SelectedOwner = option;
						IntializeActorPreview(widget, world, worldRenderer); });
				item.Get<LabelWidget>("LABEL").GetText = () => option;
				item.GetColor = () => player.Color.RGB; // TODO: this is overwritten with the chrome metrics default
				return item;
			};

			ownersDropDown.OnClick = () =>
				ownersDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 270, ownernames, setupItem);

			ownersDropDown.Text = ownernames.First();
			ownersDropDown.TextColor = world.Players.First(p => p.InternalName == ownernames.First()).Color.RGB;
			editor.SelectedOwner = ownersDropDown.Text;

			IntializeActorPreview(widget, world, worldRenderer);
		}

		void IntializeActorPreview(Widget widget, World world, WorldRenderer worldRenderer)
		{
			panel.RemoveChildren();

			var actors = modRules.Actors.Where(a => !a.Value.Name.Contains('^') && !a.Value.Name.Contains('.'))
				.Select(a => a.Value);

			foreach (var a in actors)
			{
				var actor = a;
				if (actor.Traits.Contains<BridgeInfo>()) // bridge layer takes care about that automatically
					continue;

				if (!actor.Traits.Contains<IRenderActorPreviewInfo>())
					continue;

				var filter = actor.Traits.GetOrDefault<EditorTilesetFilterInfo>();
				if (filter != null)
				{
					if (filter.ExcludeTilesets != null && filter.ExcludeTilesets.Contains(world.TileSet.Id))
						continue;
					if (filter.RequireTilesets != null && !filter.RequireTilesets.Contains(world.TileSet.Id))
						continue;
				}

				var td = new TypeDictionary();
				td.Add(new FacingInit(92));
				td.Add(new TurretFacingInit(92));

				var player = world.Players.FirstOrDefault(p => p.InternalName == editor.SelectedOwner) ?? world.Players.First();

				try
				{
					var item = ScrollItemWidget.Setup(itemTemplate,
						() => actor.Name == editor.SelectedActor,
						() => { editor.SelectedActor = actor.Name; });

					var preview = item.Get<ActorPreviewWidget>("ACTOR_PREVIEW");
					preview.SetPreview(actor, player, td);

					// Scale templates to fit within the panel
					var scale = 1f;
					if (scale * preview.IdealPreviewSize.X > itemTemplate.Bounds.Width)
						scale = (float)(itemTemplate.Bounds.Width - panel.ItemSpacing) / (float)preview.IdealPreviewSize.X;

					preview.GetScale = () => scale;
					preview.Bounds.Width = (int)(scale * preview.IdealPreviewSize.X);
					preview.Bounds.Height = (int)(scale * preview.IdealPreviewSize.Y);

					item.Bounds.Width = preview.Bounds.Width + 2 * preview.Bounds.X;
					item.Bounds.Height = preview.Bounds.Height + 2 * preview.Bounds.Y;
					item.IsVisible = () => true;
					item.GetTooltipText = () => actor.Name;
					panel.AddChild(item);
				}
				catch
				{
					Log.Write("debug", "Map editor ignoring actor {0}, because of missing sprites for tileset {1}.",
						actor.Name, world.TileSet.Id);
					continue;
				}
			}
		}
	}
}
