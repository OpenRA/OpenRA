#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MapEditorLogic
	{
		[ObjectCreator.UseCtor]
		public MapEditorLogic(Widget widget, Action onExit, World world)
		{
			var newMap = world.Map;

			var title = widget.GetOrNull<TextFieldWidget>("TITLE");
			if (title != null)
			{
				title.Text = newMap.Title;
			}

			var description = widget.GetOrNull<TextFieldWidget>("DESCRIPTION");
			if (description != null)
			{
				description.Text = newMap.Description;
			}

			var author = widget.GetOrNull<TextFieldWidget>("AUTHOR");
			if (author != null)
			{
				author.Text = newMap.Author;
			}

			var showMap = widget.GetOrNull<CheckboxWidget>("MAPCHOOSER_CHECKBOX");
			if (showMap != null)
			{
				showMap.IsChecked = () => newMap.Selectable;
				showMap.OnClick = () => newMap.Selectable ^= true;
			}

			var shellmap = widget.GetOrNull<CheckboxWidget>("SHELLMAP_CHECKBOX");
			if (shellmap != null)
			{
				shellmap.IsChecked = () => newMap.UseAsShellmap;
				shellmap.OnClick = () => newMap.UseAsShellmap ^= true;
			}

			var defaultPath = new string[] {
				Platform.SupportDir, "maps", WidgetUtils.ActiveModId(),
				world.Map.Title.ToLower().Trim() }.Aggregate(Path.Combine) + ".oramap";
			var path = widget.GetOrNull<TextFieldWidget>("PATH");
			if (path != null)
			{
				path.Text = defaultPath;
			}

			var close = widget.GetOrNull<ButtonWidget>("CLOSE");
			if (close != null)
				close.OnClick = () => { Ui.CloseWindow(); onExit(); };

			var save = widget.GetOrNull<ButtonWidget>("SAVE");
			if (save != null && !string.IsNullOrEmpty(path.Text))
			{
				save.OnClick = () => {
					newMap.Title = title.Text;
					newMap.Description = description.Text;
					newMap.Author = author.Text;
					newMap.Save(path.Text);
					Game.Debug("Saved current map as {0}", path.Text);
				};
			}
		}
	}
}
