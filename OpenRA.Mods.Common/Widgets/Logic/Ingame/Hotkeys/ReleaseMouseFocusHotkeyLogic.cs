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

using System.Collections.Generic;
using OpenRA.Mods.Common.Lint;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	[ChromeLogicArgsHotkeys("ReleaseMouseFocusKey")]
	public class ReleaseMouseFocusHotkeyLogic : SingleHotkeyBaseLogic
	{
		[ObjectCreator.UseCtor]
		public ReleaseMouseFocusHotkeyLogic(Widget widget, ModData modData, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "ReleaseMouseFocusKey", "GLOBAL_KEYHANDLER", logicArgs)
		{
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			// Releasing is pointless (and would prevent the window grab from being
			// restored on the next click) when the mouse is not locked to the window.
			if (!Game.Settings.Game.LockMouseWindow)
				return false;

			// Temporarily release the mouse so that it can leave the window.
			// The grab is restored by the platform layer when the window is clicked
			// or regains focus.
			Game.Renderer.ReleaseWindowMouseFocus();

			return true;
		}
	}
}
