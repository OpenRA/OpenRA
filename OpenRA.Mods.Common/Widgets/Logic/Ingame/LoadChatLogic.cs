#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LoadChatLogic
	{
		[ObjectCreator.UseCtor]
		public LoadChatLogic(Widget widget, World world)
		{
			var ingameRoot = widget.Get("INGAME_ROOT");
			var worldRoot = ingameRoot.Get("WORLD_ROOT");

			if (world.Type == WorldType.Regular)
				Game.LoadWidget(world, "CHAT_PANEL", worldRoot, new WidgetArgs());
		}
	}
}
