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
		public SaveMapLogic(Widget widget, Action onExit, World world)
		{
			var newMap = world.Map;

			var title = widget.GetOrNull<TextFieldWidget>("TITLE");
			if (title != null)
				title.Text = newMap.Title;

			var description = widget.GetOrNull<TextFieldWidget>("DESCRIPTION");
			if (description != null)
				description.Text = newMap.Description;

			var author = widget.GetOrNull<TextFieldWidget>("AUTHOR");
			if (author != null)
				author.Text = newMap.Author;

			var visibilityDropdown = widget.GetOrNull<DropDownButtonWidget>("CLASS_DROPDOWN");
			if (visibilityDropdown != null)
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
				visibilityDropdown.Text = Enum.GetName(typeof(MapVisibility), newMap.Visibility);
				visibilityDropdown.OnClick = () =>
					visibilityDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, mapVisibility, setupItem);
			}

			var pathDropdown = widget.GetOrNull<DropDownButtonWidget>("PATH_DROPDOWN");
			if (pathDropdown != null)
			{
				var mapFolders = new List<string>();
				foreach (var mapFolder in Game.ModData.Manifest.MapFolders.Keys)
				{
					var folder = mapFolder;
					if (mapFolder.StartsWith("~"))
						folder = mapFolder.Substring(1);

					mapFolders.Add(Platform.ResolvePath(folder));
				}

				Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template,
						() => pathDropdown.Text == Platform.UnresolvePath(option),
						() => { pathDropdown.Text = Platform.UnresolvePath(option); });
					item.Get<LabelWidget>("LABEL").GetText = () => option;
					return item;
				};

				var userMapFolder = Game.ModData.Manifest.MapFolders.First(f => f.Value == "User").Key;
				if (userMapFolder.StartsWith("~"))
					userMapFolder = userMapFolder.Substring(1);
				pathDropdown.Text = Platform.UnresolvePath(userMapFolder);
				pathDropdown.OnClick = () =>
					pathDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, mapFolders, setupItem);
			}

			var filename = widget.GetOrNull<TextFieldWidget>("FILENAME");
			if (filename != null)
				filename.Text = Path.GetFileName(world.Map.Path);

			var close = widget.GetOrNull<ButtonWidget>("CLOSE");
			if (close != null)
				close.OnClick = () => { Ui.CloseWindow(); onExit(); };

			var save = widget.GetOrNull<ButtonWidget>("SAVE");
			if (save != null && !string.IsNullOrEmpty(filename.Text))
			{
				var editorLayer = world.WorldActor.Trait<EditorActorLayer>();
				save.OnClick = () =>
				{
					newMap.Title = title.Text;
					newMap.Description = description.Text;
					newMap.Author = author.Text;
					newMap.Visibility = (MapVisibility)Enum.Parse(typeof(MapVisibility), visibilityDropdown.Text);
					newMap.ActorDefinitions = editorLayer.Save();
					newMap.PlayerDefinitions = editorLayer.Players.ToMiniYaml();
					newMap.RequiresMod = Game.ModData.Manifest.Mod.Id;

					var combinedPath = Path.Combine(pathDropdown.Text, filename.Text);
					var resolvedPath = Platform.ResolvePath(combinedPath);
					newMap.Save(resolvedPath);

					// Update the map cache so it can be loaded without restarting the game
					Game.ModData.MapCache[newMap.Uid].UpdateFromMap(newMap, MapClassification.User);

					Console.WriteLine("Saved current map at {0}", resolvedPath);
					Ui.CloseWindow();
					onExit();
				};
			}
		}
	}
}
