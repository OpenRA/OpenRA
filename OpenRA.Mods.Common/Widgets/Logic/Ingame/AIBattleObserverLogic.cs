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

using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	/// <summary>
	/// Logic for the AI Battle observer speed control panel.
	/// Provides pause/play and speed multiplier buttons for controlling the simulation speed.
	/// Also loads the AI Battle replay overlay when viewing AI Battle replays.
	/// </summary>
	public class AIBattleObserverLogic : ChromeLogic
	{
		readonly World world;
		readonly int originalTimestep;

		[ObjectCreator.UseCtor]
		public AIBattleObserverLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			originalTimestep = world.Timestep;

			// Store the base timestep in the state
			AIBattleState.BaseTimestep = originalTimestep;

			SetupSpeedControls(widget);

			// Load the AI Battle replay overlay when viewing AI Battle replays
			if (AIBattleState.IsAIBattle && world.IsReplay)
				Game.LoadWidget(world, "AI_BATTLE_REPLAY_OVERLAY", Ui.Root, new WidgetArgs());
		}

		void SetupSpeedControls(Widget widget)
		{
			var container = widget.GetOrNull("AI_BATTLE_SPEED_CONTROLS");
			if (container == null)
				return;

			// Make the speed controls visible for AI Battle mode (not during replay)
			container.IsVisible = () => AIBattleState.IsAIBattle && !world.IsReplay;

			// Expand the background to accommodate the speed controls when in AI Battle mode
			var background = widget.Parent?.GetOrNull("OBSERVER_CONTROL_BG");
			if (background != null && AIBattleState.IsAIBattle)
				background.Bounds.Height += container.Bounds.Height;

			var pauseButton = container.GetOrNull<ButtonWidget>("BUTTON_PAUSE");
			if (pauseButton != null)
			{
				pauseButton.IsVisible = () => !AIBattleState.IsPaused;
				pauseButton.OnClick = () => AIBattleState.IsPaused = true;
			}

			var playButton = container.GetOrNull<ButtonWidget>("BUTTON_PLAY");
			if (playButton != null)
			{
				playButton.IsVisible = () => AIBattleState.IsPaused;
				playButton.OnClick = () => AIBattleState.IsPaused = false;
			}

			SetupSpeedButton(container, "BUTTON_1X", 1);
			SetupSpeedButton(container, "BUTTON_2X", 2);
			SetupSpeedButton(container, "BUTTON_4X", 4);
			SetupSpeedButton(container, "BUTTON_8X", 8);
			SetupSpeedButton(container, "BUTTON_16X", 16);
			SetupSpeedButton(container, "BUTTON_32X", 32);
			SetupSpeedButton(container, "BUTTON_64X", 64);
			SetupSpeedButton(container, "BUTTON_128X", 128);
		}

		void SetupSpeedButton(Widget container, string buttonId, int speed)
		{
			var button = container.GetOrNull<ButtonWidget>(buttonId);
			if (button == null)
				return;

			button.OnClick = () =>
			{
				AIBattleState.SpeedMultiplier = speed;
				AIBattleState.IsPaused = false;
			};
			button.IsHighlighted = () => AIBattleState.SpeedMultiplier == speed && !AIBattleState.IsPaused;
		}
	}
}
