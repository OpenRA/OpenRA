#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncMainMenuLogic : MainMenuLogic
	{
		[ObjectCreator.UseCtor]
		public CncMainMenuLogic(Widget widget, World world)
			: base(widget, world)
		{
			var shellmapDecorations = widget.Get("SHELLMAP_DECORATIONS");
			shellmapDecorations.IsVisible = () => menuType != MenuType.None && Game.Settings.Game.ShowShellmap;
			shellmapDecorations.Get<ImageWidget>("RECBLOCK").IsVisible = () => world.WorldTick / 25 % 2 == 0;

			//TODO: on lua update
			var shellmapDisabledDecorations = widget.Get("SHELLMAP_DISABLED_DECORATIONS");
			shellmapDisabledDecorations.IsVisible = () => true;
		}
	}
}
