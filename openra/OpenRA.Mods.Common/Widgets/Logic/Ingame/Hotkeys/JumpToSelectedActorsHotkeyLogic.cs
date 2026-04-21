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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Lint;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	[ChromeLogicArgsHotkeys("JumpToSelectedActorsKey")]
	public class JumpToSelectedActorsHotkeyLogic : SingleHotkeyBaseLogic
	{
		readonly Viewport viewport;
		readonly ISelection selection;

		[ObjectCreator.UseCtor]
		public JumpToSelectedActorsHotkeyLogic(Widget widget, ModData modData, WorldRenderer worldRenderer, World world, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "JumpToSelectedActorsKey", "WORLD_KEYHANDLER", logicArgs)
		{
			viewport = worldRenderer.Viewport;
			selection = world.Selection;
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			viewport.Center(selection.Actors);

			return true;
		}
	}
}
