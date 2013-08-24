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
			var title = widget.GetOrNull<TextFieldWidget>("TITLE");
			if (title != null)
			{
				title.Text = world.Map.Title;
			}

			var description = widget.GetOrNull<TextFieldWidget>("DESCRIPTION");
			if (description != null)
			{
				description.Text = world.Map.Description;
			}

			var author = widget.GetOrNull<TextFieldWidget>("AUTHOR");
			if (author != null)
			{
				author.Text = world.Map.Author;
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
					var newMap = world.Map;
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
