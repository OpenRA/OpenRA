#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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

		class SaveDirectory
		{
			public readonly Folder Folder;
			public readonly string DisplayName;
			public readonly MapClassification Classification;

			public SaveDirectory(Folder folder, string displayName, MapClassification classification)
			{
				Folder = folder;
				DisplayName = displayName;
				Classification = classification;
			}
		}

		[ObjectCreator.UseCtor]
		public SaveMapLogic(Widget widget, ModData modData, Action<string> onSave, Action onExit,
			Map map, List<MiniYamlNode> playerDefinitions, List<MiniYamlNode> actorDefinitions)
		{
			var title = widget.Get<TextFieldWidget>("TITLE");
			title.Text = map.Title;

			var author = widget.Get<TextFieldWidget>("AUTHOR");
			author.Text = map.Author;

			var visibilityPanel = Ui.LoadWidget("MAP_SAVE_VISIBILITY_PANEL", null, new WidgetArgs());
			var visOptionTemplate = visibilityPanel.Get<CheckboxWidget>("VISIBILITY_TEMPLATE");
			visibilityPanel.RemoveChildren();

			foreach (MapVisibility visibilityOption in Enum.GetValues(typeof(MapVisibility)))
			{
				// To prevent users from breaking the game only show the 'Shellmap' option when it is already set.
				if (visibilityOption == MapVisibility.Shellmap && !map.Visibility.HasFlag(visibilityOption))
					continue;

				var checkbox = (CheckboxWidget)visOptionTemplate.Clone();
				checkbox.GetText = () => visibilityOption.ToString();
				checkbox.IsChecked = () => map.Visibility.HasFlag(visibilityOption);
				checkbox.OnClick = () => map.Visibility ^= visibilityOption;
				visibilityPanel.AddChild(checkbox);
			}

			var visibilityDropdown = widget.Get<DropDownButtonWidget>("VISIBILITY_DROPDOWN");
			visibilityDropdown.OnMouseDown = _ =>
			{
				visibilityDropdown.RemovePanel();
				visibilityDropdown.AttachPanel(visibilityPanel);
			};

			var writableDirectories = new List<SaveDirectory>();
			SaveDirectory selectedDirectory = null;

			var directoryDropdown = widget.Get<DropDownButtonWidget>("DIRECTORY_DROPDOWN");
			{
				Func<SaveDirectory, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template,
						() => selectedDirectory == option,
						() => selectedDirectory = option);
					item.Get<LabelWidget>("LABEL").GetText = () => option.DisplayName;
					return item;
				};

				foreach (var kv in modData.MapCache.MapLocations)
				{
					var folder = kv.Key as Folder;
					if (folder == null)
						continue;

					try
					{
						using (var fs = File.Create(Path.Combine(folder.Name, ".testwritable"), 1, FileOptions.DeleteOnClose))
						{
							// Do nothing: we just want to test whether we can create the file
						}

						writableDirectories.Add(new SaveDirectory(folder, kv.Value.ToString(), kv.Value));
					}
					catch
					{
						// Directory is not writable
					}
				}

				if (map.Package != null)
					selectedDirectory = writableDirectories.FirstOrDefault(k => k.Folder.Contains(map.Package.Name));

				// Prioritize MapClassification.User directories over system directories
				if (selectedDirectory == null)
					selectedDirectory = writableDirectories.OrderByDescending(kv => kv.Classification).First();

				directoryDropdown.GetText = () => selectedDirectory == null ? "" : selectedDirectory.DisplayName;
				directoryDropdown.OnClick = () =>
					directoryDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, writableDirectories, setupItem);
			}

			var mapIsUnpacked = map.Package != null && map.Package is Folder;

			var filename = widget.Get<TextFieldWidget>("FILENAME");
			filename.Text = map.Package == null ? "" : mapIsUnpacked ? Path.GetFileName(map.Package.Name) : Path.GetFileNameWithoutExtension(map.Package.Name);
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
				map.Author = author.Text;

				if (actorDefinitions != null)
					map.ActorDefinitions = actorDefinitions;

				if (playerDefinitions != null)
					map.PlayerDefinitions = playerDefinitions;

				map.RequiresMod = modData.Manifest.Id;

				var combinedPath = Platform.ResolvePath(Path.Combine(selectedDirectory.Folder.Name, filename.Text + fileTypes[fileType].Extension));

				// Invalidate the old map metadata
				if (map.Uid != null && map.Package != null && map.Package.Name == combinedPath)
					modData.MapCache[map.Uid].Invalidate();

				try
				{
					var package = map.Package as IReadWritePackage;
					if (package == null || package.Name != combinedPath)
					{
						selectedDirectory.Folder.Delete(combinedPath);
						if (fileType == MapFileType.OraMap)
							package = ZipFileLoader.Create(combinedPath);
						else
							package = new Folder(combinedPath);
					}

					map.Save(package);

					// Update the map cache so it can be loaded without restarting the game
					modData.MapCache[map.Uid].UpdateFromMap(map.Package, selectedDirectory.Folder, selectedDirectory.Classification, null, map.Grid.Type);

					Console.WriteLine("Saved current map at {0}", combinedPath);
					Ui.CloseWindow();

					onSave(map.Uid);
				}
				catch (Exception e)
				{
					Log.Write("debug", "Failed to save map at {0}: {1}", combinedPath, e.Message);
					Log.Write("debug", "{0}", e.StackTrace);

					ConfirmationDialogs.ButtonPrompt(
						title: "Failed to save map",
						text: "See debug.log for details.",
						onConfirm: () => { },
						confirmText: "Ok");
				}
			};
		}
	}
}
