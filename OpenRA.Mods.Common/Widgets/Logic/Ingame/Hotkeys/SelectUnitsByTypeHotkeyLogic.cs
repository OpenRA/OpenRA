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
	[ChromeLogicArgsHotkeys("SelectUnitsByTypeKey")]
	public class SelectUnitsByTypeHotkeyLogic : SingleHotkeyBaseLogic
	{
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly ISelection selection;

		public readonly string ClickSound = ChromeMetrics.Get<string>("ClickSound");
		public readonly string ClickDisabledSound = ChromeMetrics.Get<string>("ClickDisabledSound");

		[ObjectCreator.UseCtor]
		public SelectUnitsByTypeHotkeyLogic(Widget widget, ModData modData, WorldRenderer worldRenderer, World world, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "SelectUnitsByTypeKey", "WORLD_KEYHANDLER", logicArgs)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			selection = world.Selection;
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			if (world.IsGameOver)
				return false;

			if (!selection.Actors.Any())
			{
				TextNotificationsManager.AddFeedbackLine("Nothing selected.");
				Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Sounds", ClickDisabledSound, null);

				return false;
			}

			var eligiblePlayers = SelectionUtils.GetPlayersToIncludeInSelection(world);

			var ownedActors = selection.Actors
				.Where(x => !x.IsDead && eligiblePlayers.Contains(x.Owner))
				.ToList();

			if (ownedActors.Count == 0)
				return false;

			// Get all the selected actors' selection classes
			var selectedClasses = ownedActors
				.Select(a => a.Trait<ISelectable>().Class)
				.ToHashSet();

			// Select actors on the screen that have the same selection class as one of the already selected actors
			var newSelection = SelectionUtils.SelectActorsOnScreen(world, worldRenderer, selectedClasses, eligiblePlayers).ToList();

			// Check if selecting actors on the screen has selected new units
			if (newSelection.Count > selection.Actors.Count())
			{
				if (newSelection.Count > 1)
					TextNotificationsManager.AddFeedbackLine($"Selected {newSelection.Count} units across screen.");
				else
					TextNotificationsManager.AddFeedbackLine("Selected one unit across screen.");
			}
			else
			{
				// Select actors in the world that have the same selection class as one of the already selected actors
				newSelection = SelectionUtils.SelectActorsInWorld(world, selectedClasses, eligiblePlayers).ToList();

				if (newSelection.Count > 1)
					TextNotificationsManager.AddFeedbackLine($"Selected {newSelection.Count} units across map.");
				else
					TextNotificationsManager.AddFeedbackLine("Selected one unit across map.");
			}

			selection.Combine(world, newSelection, true, false);

			Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Sounds", ClickSound, null);

			return true;
		}
	}
}
