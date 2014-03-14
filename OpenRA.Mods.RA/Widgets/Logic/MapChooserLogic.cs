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
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MapChooserLogic
	{
		Map map;

		// May be a subset of available maps if a mode filter is active
		Dictionary<string, Map> visibleMaps;

		ScrollPanelWidget scrollpanel;
		ScrollItemWidget itemTemplate;
		string gameMode;

		[ObjectCreator.UseCtor]
		internal MapChooserLogic(Widget widget, string initialMap, Action onExit, Action<Map> onSelect)
		{
			map = Game.modData.MapCache[WidgetUtils.ChooseInitialMap(initialMap)].Map;

			widget.Get<ButtonWidget>("BUTTON_OK").OnClick = () => { Ui.CloseWindow(); onSelect(map); };
			widget.Get<ButtonWidget>("BUTTON_CANCEL").OnClick = () => { Ui.CloseWindow(); onExit(); };

			scrollpanel = widget.Get<ScrollPanelWidget>("MAP_LIST");
			scrollpanel.ScrollVelocity = 40f;
			scrollpanel.Layout = new GridLayout(scrollpanel);

			itemTemplate = scrollpanel.Get<ScrollItemWidget>("MAP_TEMPLATE");

			var gameModeDropdown = widget.GetOrNull<DropDownButtonWidget>("GAMEMODE_FILTER");
			if (gameModeDropdown != null)
			{
				var selectableMaps = Game.modData.MapCache.Where(m => m.Status == MapStatus.Available && m.Map.Selectable);
				var gameModes = selectableMaps
					.GroupBy(m => m.Type)
					.Select(g => Pair.New(g.Key, g.Count())).ToList();

				// 'all game types' extra item
				gameModes.Insert(0, Pair.New(null as string, selectableMaps.Count()));

				Func<Pair<string, int>, string> showItem =
					x => "{0} ({1})".F(x.First ?? "All Game Types", x.Second);

				Func<Pair<string, int>, ScrollItemWidget, ScrollItemWidget> setupItem = (ii, template) =>
				{
					var item = ScrollItemWidget.Setup(template,
						() => gameMode == ii.First,
						() => { gameMode = ii.First; EnumerateMaps(onSelect); });
					item.Get<LabelWidget>("LABEL").GetText = () => showItem(ii);
					return item;
				};

				gameModeDropdown.OnClick = () =>
					gameModeDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, gameModes, setupItem);

				gameModeDropdown.GetText = () => showItem(gameModes.First(m => m.First == gameMode));
			}

			var randomMapButton = widget.GetOrNull<ButtonWidget>("RANDOMMAP_BUTTON");
			if (randomMapButton != null)
			{
				randomMapButton.OnClick = () =>
				{
					var kv = visibleMaps.Random(Game.CosmeticRandom);
					map = kv.Value;
					scrollpanel.ScrollToItem(kv.Key);
				};
				randomMapButton.IsDisabled = () => visibleMaps == null || visibleMaps.Count == 0;
			}

			EnumerateMaps(onSelect);
		}

		void EnumerateMaps(Action<Map> onSelect)
		{
			var maps = Game.modData.MapCache
				.Where(m => m.Status == MapStatus.Available && m.Map.Selectable)
				.ToDictionary(m => m.Uid, m => m.Map)
				.Where(kv => kv.Value.Type == gameMode || gameMode == null)
				.OrderBy(kv => kv.Value.PlayerCount)
				.ThenBy(kv => kv.Value.Title);

			scrollpanel.RemoveChildren();
			foreach (var kv in maps)
			{
				var m = kv.Value;
				var item = ScrollItemWidget.Setup(kv.Key, itemTemplate, () => m == map, () => map = m, () => { Ui.CloseWindow(); onSelect(m); });

				var titleLabel = item.Get<LabelWidget>("TITLE");
				titleLabel.GetText = () => m.Title;

				var preview = Game.modData.MapCache[m.Uid];
				var previewWidget = item.Get<MapPreviewWidget>("PREVIEW");
				previewWidget.IgnoreMouseOver = true;
				previewWidget.IgnoreMouseInput = true;
				previewWidget.Preview = () => preview;
				previewWidget.IsVisible = () => previewWidget.RenderBounds.IntersectsWith(scrollpanel.RenderBounds);

				var previewLoadingWidget = item.GetOrNull<BackgroundWidget>("PREVIEW_PLACEHOLDER");
				if (previewLoadingWidget != null)
					previewLoadingWidget.IsVisible = () => !previewWidget.Loaded;

				var detailsWidget = item.GetOrNull<LabelWidget>("DETAILS");
				if (detailsWidget != null)
					detailsWidget.GetText = () => "{0} ({1} players)".F(m.Type, m.PlayerCount);

				var authorWidget = item.GetOrNull<LabelWidget>("AUTHOR");
				if (authorWidget != null)
					authorWidget.GetText = () => "Created by {0}".F(m.Author);

				var sizeWidget = item.GetOrNull<LabelWidget>("SIZE");
				if (sizeWidget != null)
				{
					var size = m.Bounds.Width + "x" + m.Bounds.Height;
					var numberPlayableCells = m.Bounds.Width * m.Bounds.Height;
					if (numberPlayableCells >= 120 * 120) size += " (Huge)";
					else if (numberPlayableCells >= 90 * 90) size += " (Large)";
					else if (numberPlayableCells >= 60 * 60) size += " (Medium)";
					else size += " (Small)";
					sizeWidget.GetText = () => size;
				}

				scrollpanel.AddChild(item);
			}

			visibleMaps = maps.ToDictionary(kv => kv.Key, kv => kv.Value);
			if (visibleMaps.ContainsValue(map))
				scrollpanel.ScrollToItem(visibleMaps.First(m => m.Value == map).Key);
		}
	}
}
