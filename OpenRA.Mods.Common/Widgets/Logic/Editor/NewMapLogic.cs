#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class NewMapLogic : ChromeLogic
	{
		Widget panel;

		[ObjectCreator.UseCtor]
		public NewMapLogic(Action onExit, Action<string> onSelect, Widget widget, World world, ModData modData)
		{
			panel = widget;

			panel.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };

			var tilesetDropDown = panel.Get<DropDownButtonWidget>("TILESET");
			var tilesets = modData.DefaultTileSets.Select(t => t.Key).ToList();
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
			{
				var item = ScrollItemWidget.Setup(template,
					() => tilesetDropDown.Text == option,
					() => { tilesetDropDown.Text = option; });
				item.Get<LabelWidget>("LABEL").GetText = () => option;
				return item;
			};
			tilesetDropDown.Text = tilesets.First();
			tilesetDropDown.OnClick = () =>
				tilesetDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, tilesets, setupItem);

			var widthTextField = panel.Get<TextFieldWidget>("WIDTH");
			var heightTextField = panel.Get<TextFieldWidget>("HEIGHT");

			panel.Get<ButtonWidget>("CREATE_BUTTON").OnClick = () =>
			{
				int width, height;
				int.TryParse(widthTextField.Text, out width);
				int.TryParse(heightTextField.Text, out height);

				// Require at least a 2x2 playable area so that the
				// ground is visible through the edge shroud
				width = Math.Max(2, width);
				height = Math.Max(2, height);

				var maxTerrainHeight = world.Map.Grid.MaximumTerrainHeight;
				var tileset = modData.DefaultTileSets[tilesetDropDown.Text];
				var map = new Map(Game.ModData, tileset, width + 2, height + maxTerrainHeight + 2);

				var tl = new PPos(1, 1 + maxTerrainHeight);
				var br = new PPos(width, height + maxTerrainHeight);
				map.SetBounds(tl, br);

				map.PlayerDefinitions = new MapPlayers(map.Rules, 0).ToMiniYaml();
				map.FixOpenAreas();

				Action<string> afterSave = uid =>
				{
					// HACK: Work around a synced-code change check.
					// It's not clear why this is needed here, but not in the other places that load maps.
					Game.RunAfterTick(() =>
					{
						ConnectionLogic.Connect(System.Net.IPAddress.Loopback.ToString(),
							Game.CreateLocalServer(uid), "",
							() => Game.LoadEditor(uid),
							() => { Game.CloseServer(); onExit(); });
					});

					Ui.CloseWindow();
					onSelect(uid);
				};

				Ui.OpenWindow("SAVE_MAP_PANEL", new WidgetArgs()
				{
					{ "onSave", afterSave },
					{ "onExit", () => { Ui.CloseWindow(); onExit(); } },
					{ "map", map },
					{ "playerDefinitions", map.PlayerDefinitions },
					{ "actorDefinitions", map.ActorDefinitions }
				});
			};
		}
	}
}
