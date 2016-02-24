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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
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
		public SaveMapLogic(Widget widget, ModData modData, Action<string> onSave, Action onExit,
			Map map, List<MiniYamlNode> playerDefinitions, List<MiniYamlNode> actorDefinitions)
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
				if (dir.StartsWith("~"))
					dir = dir.Substring(1);

				IReadOnlyPackage package;
				string f;
				if (modData.ModFiles.TryGetPackageContaining(dir, out package, out f))
					dir = Path.Combine(package.Name, f);

				return Platform.UnresolvePath(dir);
			};

			var mapDirectories = modData.Manifest.MapFolders
				.ToDictionary(kv => makeMapDirectory(kv.Key), kv => Enum<MapClassification>.Parse(kv.Value));

			var mapPath = map.Package != null ? map.Package.Name : null;
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

				// TODO: This won't work for maps inside oramod packages
				var mapDirectory = mapPath != null ? Platform.UnresolvePath(Path.GetDirectoryName(mapPath)) : null;
				var initialDirectory = mapDirectories.Keys.FirstOrDefault(f => f == mapDirectory);

				// Prioritize MapClassification.User directories over system directories
				if (initialDirectory == null)
					initialDirectory = mapDirectories.OrderByDescending(kv => kv.Value).First().Key;

				directoryDropdown.Text = initialDirectory;
				directoryDropdown.OnClick = () =>
					directoryDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, mapDirectories.Keys, setupItem);
			}

			var mapIsUnpacked = false;

			// TODO: This won't work for maps inside oramod packages
			if (mapPath != null)
			{
				var attr = File.GetAttributes(mapPath);
				mapIsUnpacked = attr.HasFlag(FileAttributes.Directory);
			}

			var filename = widget.Get<TextFieldWidget>("FILENAME");
			filename.Text = mapIsUnpacked ? Path.GetFileName(mapPath) : Path.GetFileNameWithoutExtension(mapPath);
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

				map.RequiresMod = modData.Manifest.Mod.Id;

				// Create the map directory if required
				Directory.CreateDirectory(Platform.ResolvePath(directoryDropdown.Text));

				// TODO: This won't work for maps inside oramod packages
				var combinedPath = Platform.ResolvePath(Path.Combine(directoryDropdown.Text, filename.Text + fileTypes[fileType].Extension));

				// Invalidate the old map metadata
				if (map.Uid != null && combinedPath == mapPath)
					modData.MapCache[map.Uid].Invalidate();

				var package = map.Package as IReadWritePackage;
				if (package == null || package.Name != combinedPath)
				{
					try
					{
						if (fileType == MapFileType.OraMap)
						{
							if (File.Exists(combinedPath))
								File.Delete(combinedPath);

							package = new ZipFile(modData.DefaultFileSystem, combinedPath, true);
						}
						else
						{
							if (Directory.Exists(combinedPath))
								Directory.Delete(combinedPath, true);
							package = new Folder(combinedPath);
						}

						map.Save(package);
					}
					catch
					{
						Console.WriteLine("Failed to save map at {0}", combinedPath);
					}
				}

				// Update the map cache so it can be loaded without restarting the game
				var classification = mapDirectories[directoryDropdown.Text];
				modData.MapCache[map.Uid].UpdateFromMap(map.Package, classification, null, map.Grid.Type);

				Console.WriteLine("Saved current map at {0}", combinedPath);
				Ui.CloseWindow();

				onSave(map.Uid);
			};
		}
	}
}
