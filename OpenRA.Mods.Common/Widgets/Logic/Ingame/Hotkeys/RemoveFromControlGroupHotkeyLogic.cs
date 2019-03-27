#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Lint;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	[ChromeLogicArgsHotkeys("RemoveFromControlGroupKey")]
	public class RemoveFromControlGroupHotkeyLogic : SingleHotkeyBaseLogic
	{
		readonly Selection selection;
		readonly World world;

		[ObjectCreator.UseCtor]
		public RemoveFromControlGroupHotkeyLogic(Widget widget, ModData modData, World world, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "RemoveFromControlGroupKey", "WORLD_KEYHANDLER", logicArgs)
		{
			selection = world.Selection;
			this.world = world;
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			var selectedActors = selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && a.IsInWorld && !a.IsDead)
				.ToArray();

			foreach (var a in selectedActors)
				selection.RemoveFromControlGroup(a);

			return true;
		}
	}
}
