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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	[ChromeLogicArgsHotkeys("TogglePlayerStanceColorKey")]
	public class TogglePlayerStanceColorHotkeyLogic : SingleHotkeyBaseLogic
	{
		readonly World world;
		readonly WorldRenderer worldRenderer;

		[ObjectCreator.UseCtor]
		public TogglePlayerStanceColorHotkeyLogic(Widget widget, World world, WorldRenderer worldRenderer, ModData modData, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "TogglePlayerStanceColorKey", "WORLD_KEYHANDLER", logicArgs)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			Game.Settings.Game.UsePlayerStanceColors ^= true;
			Player.SetupRelationshipColors(world.Players, world.LocalPlayer, worldRenderer, false);

			return true;
		}
	}
}
