#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MapChooserLogic
	{
		string selectedUid;

		// May be a subset of available maps if a mode filter is active
		List<string> visibleMaps;

		ScrollPanelWidget scrollpanel;
		ScrollItemWidget itemTemplate;
		string mapFilter;
		string gameMode;

		[ObjectCreator.UseCtor]
		internal MapChooserLogic(Widget widget, string initialMap, Action onExit, Action<string> onSelect)
		{
			selectedUid = WidgetUtils.ChooseInitialMap(initialMap);

			widget.Get<ButtonWidget>("BUTTON_OK").OnClick = () => { Ui.CloseWindow(); onSelect(selectedUid); };
			widget.Get<ButtonWidget>("BUTTON_CANCEL").OnClick = () => { Ui.CloseWindow(); onExit(); };

			scrollpanel = widget.Get<ScrollPanelWidget>("MAP_LIST");
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

			mapfilterInput = widget.GetOrNull<TextFieldWidget>("MAPFILTER_INPUT");
			if (mapfilterInput != null)
			{
				mapfilterInput.OnTextEdited = () => { mapFilter = mapfilterInput.Text; EnumerateMaps(onSelect); };
			}

			var randomMapButton = widget.GetOrNull<ButtonWidget>("RANDOMMAP_BUTTON");
			if (randomMapButton != null)
			{
				randomMapButton.OnClick = () =>
				{
					var uid = visibleMaps.Random(Game.CosmeticRandom);
					selectedUid = uid;
					scrollpanel.ScrollToItem(uid, smooth: true);
				};
				randomMapButton.IsDisabled = () => visibleMaps == null || visibleMaps.Count == 0;
			}

			EnumerateMaps(onSelect);
		}

		void EnumerateMaps(Action<string> onSelect)
		{
			var maps = Game.modData.MapCache
				.Where(m => m.Status == MapStatus.Available && m.Map.Selectable)
				.Where(m => gameMode == null || m.Type == gameMode)
				.Where(m => mapFilter == null || m.Title.IndexOf(mapFilter, StringComparison.OrdinalIgnoreCase) >= 0 || m.Author.IndexOf(mapFilter, StringComparison.OrdinalIgnoreCase) >= 0)
				.OrderBy(m => m.PlayerCount)
				.ThenBy(m => m.Title);

			scrollpanel.RemoveChildren();
			foreach (var loop in maps)
			{
				var preview = loop;

				// Access the minimap to trigger async generation of the minimap.
				preview.GetMinimap();

				var item = ScrollItemWidget.Setup(preview.Uid, itemTemplate, () => selectedUid == preview.Uid, () => selectedUid = preview.Uid, () => { Ui.CloseWindow(); onSelect(preview.Uid); });
				item.IsVisible = () => item.RenderBounds.IntersectsWith(scrollpanel.RenderBounds);

				var titleLabel = item.Get<LabelWidget>("TITLE");
				titleLabel.GetText = () => preview.Title;

				var previewWidget = item.Get<MapPreviewWidget>("PREVIEW");
				previewWidget.Preview = () => preview;

				var detailsWidget = item.GetOrNull<LabelWidget>("DETAILS");
				if (detailsWidget != null)
					detailsWidget.GetText = () => "{0} ({1} players)".F(preview.Type, preview.PlayerCount);

				var authorWidget = item.GetOrNull<LabelWidget>("AUTHOR");
				if (authorWidget != null)
					authorWidget.GetText = () => "Created by {0}".F(preview.Author);

				var sizeWidget = item.GetOrNull<LabelWidget>("SIZE");
				if (sizeWidget != null)
				{
					var size = preview.Bounds.Width + "x" + preview.Bounds.Height;
					var numberPlayableCells = preview.Bounds.Width * preview.Bounds.Height;
					if (numberPlayableCells >= 120 * 120) size += " (Huge)";
					else if (numberPlayableCells >= 90 * 90) size += " (Large)";
					else if (numberPlayableCells >= 60 * 60) size += " (Medium)";
					else size += " (Small)";
					sizeWidget.GetText = () => size;
				}

				scrollpanel.AddChild(item);
			}

			visibleMaps = maps.Select(m => m.Uid).ToList();
			if (visibleMaps.Contains(selectedUid))
				scrollpanel.ScrollToItem(selectedUid);
		}
	}
}
