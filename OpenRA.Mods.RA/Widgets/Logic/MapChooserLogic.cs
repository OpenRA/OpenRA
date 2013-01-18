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
using System.Threading;
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
		Thread mapLoaderThread;

		[ObjectCreator.UseCtor]
		internal MapChooserLogic(Widget widget, string initialMap, Action onExit, Action<Map> onSelect)
		{
			map = Game.modData.AvailableMaps[WidgetUtils.ChooseInitialMap(initialMap)];

			widget.Get<ButtonWidget>("BUTTON_OK").OnClick = () => { Ui.CloseWindow(); onSelect(map); };
			widget.Get<ButtonWidget>("BUTTON_CANCEL").OnClick = () => { Ui.CloseWindow(); onExit(); };

			scrollpanel = widget.Get<ScrollPanelWidget>("MAP_LIST");
			scrollpanel.ScrollVelocity = 40f;

			itemTemplate = scrollpanel.Get<ScrollItemWidget>("MAP_TEMPLATE");

			var gameModeDropdown = widget.GetOrNull<DropDownButtonWidget>("GAMEMODE_FILTER");
			if (gameModeDropdown != null)
			{
				var selectableMaps = Game.modData.AvailableMaps.Where(m => m.Value.Selectable).ToList();
				var gameModes = selectableMaps
					.GroupBy(m => m.Value.Type)
					.Select(g => Pair.New(g.Key, g.Count())).ToList();

				// 'all game types' extra item
				gameModes.Insert(0, Pair.New(null as string, selectableMaps.Count()));

				Func<Pair<string, int>, string> showItem =
					x => "{0} ({1})".F(x.First ?? "All Game Types", x.Second);

				Func<Pair<string, int>, ScrollItemWidget, ScrollItemWidget> setupItem = (ii, template) =>
				{
					var item = ScrollItemWidget.Setup(template,
						() => gameMode == ii.First,
						() => { gameMode = ii.First; EnumerateMapsAsync(); });
					item.Get<LabelWidget>("LABEL").GetText = () => showItem(ii);
					return item;
				};

				gameModeDropdown.OnClick = () =>
					gameModeDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, gameModes, setupItem);

				gameModeDropdown.GetText = () => showItem(gameModes.First(m => m.First == gameMode));
			}

			EnumerateMapsAsync();
		}

		void EnumerateMapsAsync()
		{
			if (mapLoaderThread != null && mapLoaderThread.IsAlive) 
				mapLoaderThread.Abort(); // violent, but should be fine since we are not doing anything sensitive in this thread

			mapLoaderThread = new Thread(EnumerateMaps);
			mapLoaderThread.Start();
		}

		void EnumerateMaps()
		{
			Game.RunAfterTick(() => scrollpanel.RemoveChildren()); // queue removal in case another thread added any items to the game queue
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

				var titleLabel = item.Get<LabelWidget>("TITLE");
				titleLabel.GetText = () => m.Title;

				var previewWidget = item.Get<MapPreviewWidget>("PREVIEW");
				previewWidget.IgnoreMouseOver = true;
				previewWidget.IgnoreMouseInput = true;
				previewWidget.Map = () => m;
				previewWidget.LoadMapPreview();

				var detailsWidget = item.Get<LabelWidget>("DETAILS");
				if (detailsWidget != null)
					detailsWidget.GetText = () => "{0} ({1})".F(m.Type, m.PlayerCount);

				var authorWidget = item.Get<LabelWidget>("AUTHOR");
				if (authorWidget != null)
					authorWidget.GetText = () => m.Author;

				var sizeWidget = item.Get<LabelWidget>("SIZE");
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

				Game.RunAfterTick(() => scrollpanel.AddChild(item));
			}
		}
	}
}
