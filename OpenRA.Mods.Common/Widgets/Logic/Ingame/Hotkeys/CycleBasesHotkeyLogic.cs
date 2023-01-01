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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	[ChromeLogicArgsHotkeys("CycleBasesKey")]
	public class CycleBasesHotkeyLogic : SingleHotkeyBaseLogic
	{
		readonly Viewport viewport;
		readonly ISelection selection;
		readonly World world;

		[ObjectCreator.UseCtor]
		public CycleBasesHotkeyLogic(Widget widget, ModData modData, WorldRenderer worldRenderer, World world, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "CycleBasesKey", "WORLD_KEYHANDLER", logicArgs)
		{
			viewport = worldRenderer.Viewport;
			selection = world.Selection;
			this.world = world;
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			var player = world.RenderPlayer ?? world.LocalPlayer;

			var bases = world.ActorsHavingTrait<BaseBuilding>()
				.Where(a => a.Owner == player)
				.OrderByDescending(a => a.IsPrimaryBuilding())
				.ThenBy(a => a.ActorID)
				.ToList();

			// If no BaseBuilding exist pick the first selectable Building.
			if (bases.Count == 0)
			{
				var building = world.ActorsHavingTrait<Building>()
					.FirstOrDefault(a => a.Owner == player && a.Info.HasTraitInfo<SelectableInfo>());

				// No buildings left
				if (building == null)
					return true;

				selection.Combine(world, new Actor[] { building }, false, true);
				viewport.Center(selection.Actors);
				return true;
			}

			var next = bases
				.SkipWhile(b => !selection.Contains(b))
				.Skip(1)
				.FirstOrDefault();

			if (next == null)
				next = bases.First();

			selection.Combine(world, new Actor[] { next }, false, true);
			viewport.Center(selection.Actors);

			return true;
		}
	}
}
