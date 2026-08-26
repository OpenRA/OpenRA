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
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Mods.Tcd.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Tcd.Widgets.Logic
{
	[ChromeLogicArgsHotkeys("FormSquadKey")]
	public sealed class FormSquadHotkeyLogic : SingleHotkeyBaseLogic
	{
		readonly World world;

		[ObjectCreator.UseCtor]
		public FormSquadHotkeyLogic(Widget widget, ModData modData, World world, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "FormSquadKey", "PLAYER_KEYHANDLER", logicArgs)
		{
			this.world = world;
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			var squads = world.WorldActor.TraitOrDefault<SquadManager>();
			if (squads == null)
				return false;

			var squad = squads.Form(world.Selection.Actors);
			if (squad != null)
				TextNotificationsManager.Debug($"Squad {squad.Id} formed: {squad.Members.Count} units.");

			return true;
		}
	}
}
