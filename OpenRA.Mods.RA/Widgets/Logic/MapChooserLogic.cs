#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MapChooserLogic
	{
		Map map;
		ScrollPanelWidget scrollpanel;
		ScrollItemWidget itemTemplate;
		string gameMode;

		[ObjectCreator.UseCtor]
		internal MapChooserLogic([ObjectCreator.Param] Widget widget,
			[ObjectCreator.Param] string initialMap,
			[ObjectCreator.Param] Action onExit,
			[ObjectCreator.Param] Action<Map> onSelect)
		{
			map = Game.modData.AvailableMaps[WidgetUtils.ChooseInitialMap(initialMap)];

			var mapPreview = widget.GetWidget<MapPreviewWidget>("MAP_PREVIEW");
			if (mapPreview != null)
				mapPreview.Map = () => map;

			if (WidgetUtils.ActiveModTitle() != "Red Alert")	// hack
			{
				widget.GetWidget<LabelWidget>("CURMAP_TITLE").GetText = () => map.Title;
				widget.GetWidget<LabelWidget>("CURMAP_AUTHOR").GetText = () => map.Author;
				widget.GetWidget<LabelWidget>("CURMAP_DESC").GetText = () => map.Description;
				widget.GetWidget<LabelWidget>("CURMAP_DESC_LABEL").IsVisible = () => map.Description != null;
				widget.GetWidget<LabelWidget>("CURMAP_SIZE").GetText = () => "{0}x{1}".F(map.Bounds.Width, map.Bounds.Height);
				widget.GetWidget<LabelWidget>("CURMAP_THEATER").GetText = () => Rules.TileSets[map.Tileset].Name;
				widget.GetWidget<LabelWidget>("CURMAP_PLAYERS").GetText = () => map.PlayerCount.ToString();
			}

			widget.GetWidget<ButtonWidget>("BUTTON_OK").OnClick = () => { Widget.CloseWindow(); onSelect(map); };
			widget.GetWidget<ButtonWidget>("BUTTON_CANCEL").OnClick = () => { Widget.CloseWindow(); onExit(); };

			scrollpanel = widget.GetWidget<ScrollPanelWidget>("MAP_LIST");
			itemTemplate = scrollpanel.GetWidget<ScrollItemWidget>("MAP_TEMPLATE");

			var gameModeDropdown = widget.GetWidget<DropDownButtonWidget>("GAMEMODE_FILTER");
			if (gameModeDropdown != null)
			{
				var selectableMaps = Game.modData.AvailableMaps.Where(m => m.Value.Selectable);
				var gameModes = selectableMaps
					.GroupBy(m => m.Value.Type)
					.Select(g => Pair.New(g.Key, g.Count())).ToList();

				// 'all game types' extra item
				gameModes.Insert( 0, Pair.New( null as string, selectableMaps.Count() ) );

				Func<Pair<string,int>, string> showItem =
					x => "{0} ({1})".F( x.First ?? "All Game Types", x.Second );

				Func<Pair<string,int>, ScrollItemWidget, ScrollItemWidget> setupItem = (ii, template) =>
				{
					var item = ScrollItemWidget.Setup(template,
						() => gameMode == ii.First,
						() => { gameMode = ii.First; EnumerateMaps(); });
					item.GetWidget<LabelWidget>("LABEL").GetText = () => showItem(ii);
					return item;
				};

				gameModeDropdown.OnClick = () =>
					gameModeDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, gameModes, setupItem);

				gameModeDropdown.GetText = () => showItem(gameModes.First(m => m.First == gameMode));
			}

			EnumerateMaps();
		}

		void EnumerateMaps()
		{
			scrollpanel.RemoveChildren();

			// hack for RA's new 2d mapchooser
			if (WidgetUtils.ActiveModTitle() == "Red Alert")
				scrollpanel.Layout = new GridLayout(scrollpanel);

			scrollpanel.ScrollToTop();

			var maps = Game.modData.AvailableMaps
				.Where(kv => kv.Value.Selectable)
				.Where(kv => kv.Value.Type == gameMode || gameMode == null)
				.OrderBy(kv => kv.Value.PlayerCount)
				.ThenBy(kv => kv.Value.Title);

			foreach (var kv in maps)
			{
				var m = kv.Value;
				var item = ScrollItemWidget.Setup(itemTemplate, () => m == map, () => map = m);

				var titleLabel = item.GetWidget<LabelWidget>("TITLE");
				titleLabel.GetText = () => m.Title;

				var playersLabel = item.GetWidget<LabelWidget>("PLAYERS");
				if (playersLabel != null)
					playersLabel.GetText = () => "{0}".F(m.PlayerCount);

				var previewWidget = item.GetWidget<MapPreviewWidget>("PREVIEW");
				if (previewWidget != null)
				{
					previewWidget.IgnoreMouseOver = true;
					previewWidget.IgnoreMouseInput = true;
					previewWidget.Map = () => m;
				}

				var typeWidget = item.GetWidget<LabelWidget>("TYPE");
				if (typeWidget != null)
					typeWidget.GetText = () => m.Type;

				var detailsWidget = item.GetWidget<LabelWidget>("DETAILS");
				if (detailsWidget != null)
					detailsWidget.GetText = () => "{0} ({1})".F(m.Type, m.PlayerCount);

				var authorWidget = item.GetWidget<LabelWidget>("AUTHOR");
				if (authorWidget != null)
					authorWidget.GetText = () => m.Author;

				scrollpanel.AddChild(item);
			}
		}
	}
}
