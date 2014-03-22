#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ControlGroupLogic
	{
		[ObjectCreator.UseCtor]
		public ControlGroupLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			var keyhandler = widget.Get<LogicKeyListenerWidget>("CONTROLGROUP_KEYHANDLER");
			keyhandler.OnKeyPress = e =>
			{
				if (e.Key >= Keycode.NUMBER_0 && e.Key <= Keycode.NUMBER_9)
				{
					var group = (int)e.Key - (int)Keycode.NUMBER_0;
					world.Selection.DoControlGroup(world, worldRenderer, group, e.Modifiers, e.MultiTapCount);
					return true;
				}

				return false;
			};
		}
	}
}
