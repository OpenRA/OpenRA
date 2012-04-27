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
		internal MapChooserLogic(Widget widget, string initialMap, Action onExit, Action<Map> onSelect)
		{
			map = Game.modData.AvailableMaps[WidgetUtils.ChooseInitialMap(initialMap)];

			widget.Get<ButtonWidget>("BUTTON_OK").OnClick = () => { Ui.CloseWindow(); onSelect(map); };
			widget.Get<ButtonWidget>("BUTTON_CANCEL").OnClick = () => { Ui.CloseWindow(); onExit(); };

			scrollpanel = widget.Get<ScrollPanelWidget>("MAP_LIST");
			itemTemplate = scrollpanel.Get<ScrollItemWidget>("MAP_TEMPLATE");

			var gameModeDropdown = widget.GetOrNull<DropDownButtonWidget>("GAMEMODE_FILTER");
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
					item.Get<LabelWidget>("LABEL").GetText = () => showItem(ii);
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

				var detailsWidget = item.Get<LabelWidget>("DETAILS");
				if (detailsWidget != null)
					detailsWidget.GetText = () => "{0} ({1})".F(m.Type, m.PlayerCount);

				var authorWidget = item.Get<LabelWidget>("AUTHOR");
				if (authorWidget != null)
					authorWidget.GetText = () => m.Author;

				scrollpanel.AddChild(item);
			}
		}
	}
}
