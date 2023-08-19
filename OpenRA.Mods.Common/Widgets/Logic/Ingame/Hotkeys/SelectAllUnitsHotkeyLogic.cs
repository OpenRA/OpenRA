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
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	[ChromeLogicArgsHotkeys("SelectAllUnitsKey")]
	public class SelectAllUnitsHotkeyLogic : SingleHotkeyBaseLogic
	{
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly ISelection selection;

		public readonly string ClickSound = ChromeMetrics.Get<string>("ClickSound");

		[TranslationReference("units")]
		const string SelectedUnitsAcrossScreen = "selected-units-across-screen";

		[TranslationReference("units")]
		const string SelectedUnitsAcrossMap = "selected-units-across-map";

		[ObjectCreator.UseCtor]
		public SelectAllUnitsHotkeyLogic(Widget widget, ModData modData, WorldRenderer worldRenderer, World world, Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "SelectAllUnitsKey", "WORLD_KEYHANDLER", logicArgs)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			selection = world.Selection;
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			if (world.IsGameOver)
				return false;

			var eligiblePlayers = SelectionUtils.GetPlayersToIncludeInSelection(world);

			// Select actors on the screen which belong to the current player(s)
			var newSelection = SelectionUtils.SelectActorsOnScreen(world, worldRenderer, null, eligiblePlayers).SubsetWithHighestSelectionPriority(e.Modifiers).ToList();

			// Check if selecting actors on the screen has selected new units
			if (newSelection.Count > selection.Actors.Count())
				TextNotificationsManager.AddFeedbackLine(SelectedUnitsAcrossScreen, Translation.Arguments("units", newSelection.Count));
			else
			{
				// Select actors in the world that have highest selection priority
				newSelection = SelectionUtils.SelectActorsInWorld(world, null, eligiblePlayers).SubsetWithHighestSelectionPriority(e.Modifiers).ToList();
				TextNotificationsManager.AddFeedbackLine(SelectedUnitsAcrossMap, Translation.Arguments("units", newSelection.Count));
			}

			selection.Combine(world, newSelection, false, false);

			Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Sounds", ClickSound, null);

			return true;
		}
	}
}
