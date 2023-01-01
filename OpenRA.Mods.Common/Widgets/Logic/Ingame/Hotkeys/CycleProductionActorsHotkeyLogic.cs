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
	[ChromeLogicArgsHotkeys("CycleProductionActorsKey")]
	public class CycleProductionActorsHotkeyLogic : SingleHotkeyBaseLogic
	{
		readonly Viewport viewport;
		readonly ISelection selection;
		readonly World world;

		readonly string clickSound = ChromeMetrics.Get<string>("ClickSound");

		[ObjectCreator.UseCtor]
		public CycleProductionActorsHotkeyLogic(Widget widget, ModData modData, WorldRenderer worldRenderer, World world, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "CycleProductionActorsKey", "WORLD_KEYHANDLER", logicArgs)
		{
			viewport = worldRenderer.Viewport;
			selection = world.Selection;
			this.world = world;

			if (logicArgs.TryGetValue("ClickSound", out var yaml))
				clickSound = yaml.Value;
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			var player = world.RenderPlayer ?? world.LocalPlayer;

			var facilities = world.ActorsHavingTrait<Production>()
				.Where(a => a.Owner == player && a.OccupiesSpace != null && !a.Info.HasTraitInfo<BaseBuildingInfo>()
					&& a.TraitsImplementing<Production>().Any(t => !t.IsTraitDisabled))
				.OrderBy(f => f.TraitsImplementing<Production>().First(t => !t.IsTraitDisabled).Info.Produces.First())
				.ToList();

			if (facilities.Count == 0)
				return true;

			var next = facilities
				.SkipWhile(b => !selection.Contains(b))
				.Skip(1)
				.FirstOrDefault();

			if (next == null)
				next = facilities.First();

			Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", clickSound, null);

			selection.Combine(world, new Actor[] { next }, false, true);
			viewport.Center(selection.Actors);

			return true;
		}
	}
}
