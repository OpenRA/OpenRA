#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LoadMapEditorLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public LoadMapEditorLogic(Widget widget, World world)
		{
			var editorRoot = widget.Get("WORLD_ROOT");
			Game.LoadWidget(world, "EDITOR_WORLD_ROOT", editorRoot, new WidgetArgs());
			Game.LoadWidget(world, "TRANSIENTS_PANEL", editorRoot, new WidgetArgs());
		}
	}
}
