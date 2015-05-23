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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class NewMapLogic
	{
		Widget panel;

		[ObjectCreator.UseCtor]
		public NewMapLogic(Action onExit, Action<string> onSelect, Ruleset modRules, Widget widget, World world)
		{
			panel = widget;

			panel.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };

			var tilesetDropDown = panel.Get<DropDownButtonWidget>("TILESET");
			var tilesets = modRules.TileSets.Select(t => t.Key).ToList();
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
				var tileset = modRules.TileSets[tilesetDropDown.Text];
				var map = Map.FromTileset(tileset);

				int width, height;
				int.TryParse(widthTextField.Text, out width);
				int.TryParse(heightTextField.Text, out height);

				// Require at least a 2x2 playable area so that the
				// ground is visible through the edge shroud
				width = Math.Max(2, width);
				height = Math.Max(2, height);

				map.Resize(width + 2, height + tileset.MaxGroundHeight + 2);
				map.ResizeCordon(1, 1, width + 1, height + tileset.MaxGroundHeight + 1);
				map.PlayerDefinitions = new MapPlayers(map.Rules, map.SpawnPoints.Value.Length).ToMiniYaml();
				map.FixOpenAreas(modRules);

				var userMapFolder = Game.ModData.Manifest.MapFolders.First(f => f.Value == "User").Key;

				// Ignore optional flag
				if (userMapFolder.StartsWith("~"))
					userMapFolder = userMapFolder.Substring(1);

				var mapDir = Platform.ResolvePath(userMapFolder);
				Directory.CreateDirectory(mapDir);
				var tempLocation = Path.Combine(mapDir, "temp") + ".oramap";
				map.Save(tempLocation); // TODO: load it right away and save later properly

				var newMap = new Map(tempLocation);
				Game.ModData.MapCache[newMap.Uid].UpdateFromMap(newMap, MapClassification.User);

				ConnectionLogic.Connect(System.Net.IPAddress.Loopback.ToString(),
					Game.CreateLocalServer(newMap.Uid),
					"",
					() => { Game.LoadEditor(newMap.Uid); },
					() => { Game.CloseServer(); onExit(); });
				onSelect(newMap.Uid);
			};
		}
	}
}
