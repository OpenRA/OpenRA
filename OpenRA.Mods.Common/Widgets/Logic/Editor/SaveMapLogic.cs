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
	public class SaveMapLogic
	{
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

				if (initialDirectory == null)
					initialDirectory = mapDirectories.Keys.First();

				directoryDropdown.Text = initialDirectory;
				directoryDropdown.OnClick = () =>
					directoryDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, mapDirectories.Keys, setupItem);
			}

			var filename = widget.Get<TextFieldWidget>("FILENAME");
			filename.Text = Path.GetFileNameWithoutExtension(map.Path);

			var fileTypes = new Dictionary<string, string>()
			{
				{ ".oramap", ".oramap" },
				{ "(unpacked)", "" }
			};

			var typeDropdown = widget.Get<DropDownButtonWidget>("TYPE_DROPDOWN");
			{
				Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template,
						() => typeDropdown.Text == option,
						() => typeDropdown.Text = option);
					item.Get<LabelWidget>("LABEL").GetText = () => option;
					return item;
				};

				typeDropdown.Text = map.Path != null ? Path.GetExtension(map.Path) : ".oramap";
				if (string.IsNullOrEmpty(typeDropdown.Text))
					typeDropdown.Text = fileTypes.First(t => t.Value == "").Key;

				typeDropdown.OnClick = () =>
					typeDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, fileTypes.Keys, setupItem);
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

				var combinedPath = Platform.ResolvePath(Path.Combine(directoryDropdown.Text, filename.Text + fileTypes[typeDropdown.Text]));

				// Invalidate the old map metadata
				if (map.Uid != null)
					Game.ModData.MapCache[map.Uid].Invalidate();

				map.Save(combinedPath);

				// Reload map to calculate new UID
				map = new Map(combinedPath);

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
