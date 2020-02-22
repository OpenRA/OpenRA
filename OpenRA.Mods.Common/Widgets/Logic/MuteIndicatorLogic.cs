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

using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MuteIndicatorLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public MuteIndicatorLogic(Widget widget)
		{
			var gs = Game.Settings.Sound;
			widget.IsVisible = () => gs.Mute;
		}
	}
}
