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
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MapEditorLogic
	{
		[ObjectCreator.UseCtor]
		public MapEditorLogic(Widget widget, Action onExit, World world)
		{
			var close = widget.GetOrNull<ButtonWidget>("CLOSE");
			if (close != null)
				close.OnClick = () => { Ui.CloseWindow(); onExit(); };

			var title = widget.GetOrNull<TextFieldWidget>("TITLE");
			if (title != null)
			{
				title.Text = world.Map.Title;
				title.IsDisabled = () => true;
			}

			var description = widget.GetOrNull<TextFieldWidget>("DESCRIPTION");
			if (description != null)
			{
				description.Text = world.Map.Description;
				description.IsDisabled = () => true;
			}

			var author = widget.GetOrNull<TextFieldWidget>("AUTHOR");
			if (author != null)
			{
				author.Text = world.Map.Author;
				author.IsDisabled = () => true;
			}
		}
	}
}
