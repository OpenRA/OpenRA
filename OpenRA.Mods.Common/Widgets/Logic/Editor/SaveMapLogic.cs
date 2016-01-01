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
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SaveMapLogic : ChromeLogic
	{
		enum MapFileType { Unpacked, OraMap }

		struct MapFileTypeInfo
		{
			public string Extension;
			public string UiLabel;
		}

		[ObjectCreator.UseCtor]
		public SaveMapLogic(Widget widget, Action<string> onSave, Action onExit, Map map, List<MiniYamlNode> playerDefinitions, List<MiniYamlNode> actorDefinitions)
		{
			var title = widget.Get<TextFieldWidget>("TITLE");
			title.Text = map.Title;

			var author = widget.Get<TextFieldWidget>("AUTHOR");
			author.Text = map.Author;

			// TODO: This should use a multi-line textfield once they exist
			var description = widget.Get<TextFieldWidget>("DESCRIPTION");
			description.Text = map.Description;

			// TODO: This should use a multi-selection dropdown once they exist
			var visibilityDropdown = widget.Get<DropDownButtonWidget>("VISIBILITY_DROPDOWN");
			{
				var mapVisibility = new List<string>(Enum.GetNames(typeof(MapVisibility)));
				Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template,
						() => visibilityDropdown.Text == option,
						() => { visibilityDropdown.Text = option; });
					item.Get<LabelWidget>("LABEL").GetText = () => option;
					return item;
				};

				visibilityDropdown.Text = Enum.GetName(typeof(MapVisibility), map.Visibility);
				visibilityDropdown.OnClick = () =>
					visibilityDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, mapVisibility, setupItem);
			}

			Func<string, string> makeMapDirectory = dir =>
			{
				var f = Platform.UnresolvePath(dir);
				if (f.StartsWith("~"))
					f = f.Substring(1);

				return f;
			};

			var mapDirectories = Game.ModData.Manifest.MapFolders
				.ToDictionary(kv => makeMapDirectory(kv.Key), kv => Enum<MapClassification>.Parse(kv.Value));

			var directoryDropdown = widget.Get<DropDownButtonWidget>("DIRECTORY_DROPDOWN");
			{
				Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template,
						() => directoryDropdown.Text == option,
						() => directoryDropdown.Text = option);
					item.Get<LabelWidget>("LABEL").GetText = () => option;
					return item;
				};

				var mapDirectory = map.Path != null ? Platform.UnresolvePath(Path.GetDirectoryName(map.Path)) : null;
				var initialDirectory = mapDirectories.Keys.FirstOrDefault(f => f == mapDirectory);

				// Prioritize MapClassification.User directories over system directories
				if (initialDirectory == null)
					initialDirectory = mapDirectories.OrderByDescending(kv => kv.Value).First().Key;

				directoryDropdown.Text = initialDirectory;
				directoryDropdown.OnClick = () =>
					directoryDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, mapDirectories.Keys, setupItem);
			}

			var mapIsUnpacked = false;

			if (map.Path != null)
			{
				var attr = File.GetAttributes(map.Path);
				mapIsUnpacked = attr.HasFlag(FileAttributes.Directory);
			}

			var filename = widget.Get<TextFieldWidget>("FILENAME");
			filename.Text = mapIsUnpacked ? Path.GetFileName(map.Path) : Path.GetFileNameWithoutExtension(map.Path);
			var fileType = mapIsUnpacked ? MapFileType.Unpacked : MapFileType.OraMap;

			var fileTypes = new Dictionary<MapFileType, MapFileTypeInfo>()
			{
				{ MapFileType.OraMap, new MapFileTypeInfo { Extension = ".oramap", UiLabel = ".oramap" } },
				{ MapFileType.Unpacked, new MapFileTypeInfo { Extension = "", UiLabel = "(unpacked)" } }
			};

			var typeDropdown = widget.Get<DropDownButtonWidget>("TYPE_DROPDOWN");
			{
				Func<KeyValuePair<MapFileType, MapFileTypeInfo>, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template,
						() => fileType == option.Key,
						() => { typeDropdown.Text = option.Value.UiLabel; fileType = option.Key; });
					item.Get<LabelWidget>("LABEL").GetText = () => option.Value.UiLabel;
					return item;
				};

				typeDropdown.Text = fileTypes[fileType].UiLabel;

				typeDropdown.OnClick = () =>
					typeDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, fileTypes, setupItem);
			}

			var close = widget.Get<ButtonWidget>("BACK_BUTTON");
			close.OnClick = () => { Ui.CloseWindow(); onExit(); };

			var save = widget.Get<ButtonWidget>("SAVE_BUTTON");
			save.OnClick = () =>
			{
				if (string.IsNullOrEmpty(filename.Text))
					return;

				map.Title = title.Text;
				map.Description = description.Text;
				map.Author = author.Text;
				map.Visibility = (MapVisibility)Enum.Parse(typeof(MapVisibility), visibilityDropdown.Text);

				if (actorDefinitions != null)
					map.ActorDefinitions = actorDefinitions;

				if (playerDefinitions != null)
					map.PlayerDefinitions = playerDefinitions;

				map.RequiresMod = Game.ModData.Manifest.Mod.Id;

				// Create the map directory if required
				Directory.CreateDirectory(Platform.ResolvePath(directoryDropdown.Text));

				var combinedPath = Platform.ResolvePath(Path.Combine(directoryDropdown.Text, filename.Text + fileTypes[fileType].Extension));

				// Invalidate the old map metadata
				if (map.Uid != null && combinedPath == map.Path)
					Game.ModData.MapCache[map.Uid].Invalidate();

				map.Save(combinedPath);

				// Update the map cache so it can be loaded without restarting the game
				var classification = mapDirectories[directoryDropdown.Text];
				Game.ModData.MapCache[map.Uid].UpdateFromMap(map, classification);

				Console.WriteLine("Saved current map at {0}", combinedPath);
				Ui.CloseWindow();

				onSave(map.Uid);
			};
		}
	}
}
