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

using System;
using OpenRA.Mods.Common.Commands;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class DebugMenuLogic : ChromeLogic
	{
		[FluentReference("command")]
		const string TooltipDebugCommand = "tooltip-debug-command";

		[ObjectCreator.UseCtor]
		public DebugMenuLogic(Widget widget, World world)
		{
			var devTrait = world.LocalPlayer.PlayerActor.Trait<DeveloperMode>();
			var debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();
			var debugVisCommands = world.WorldActor.TraitOrDefault<DebugVisualizationCommands>();

			var visibilityCheckbox = widget.GetOrNull<CheckboxWidget>("DISABLE_VISIBILITY_CHECKS");
			if (visibilityCheckbox != null)
			{
				visibilityCheckbox.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DevCommands.Commands.Visibility);
				BindOrderCheckbox(visibilityCheckbox, world, DeveloperMode.Orders.Visibility, () => devTrait.DisableShroud);
			}

			var pathCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_UNIT_PATHS");
			if (pathCheckbox != null)
			{
				pathCheckbox.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + PathFinderOverlay.CommandName);
				BindOrderCheckbox(pathCheckbox, world, PathFinderOverlay.OrderName, () => devTrait.PathDebug);
			}

			var cashButton = widget.GetOrNull<ButtonWidget>("GIVE_CASH");
			if (cashButton != null)
			{
				cashButton.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DevCommands.Commands.GiveCash);
				cashButton.OnClick = () => IssueOrder(world, DeveloperMode.Orders.GiveCash);
			}

			var cashAllButton = widget.GetOrNull<ButtonWidget>("GIVE_CASH_ALL");
			if (cashAllButton != null)
			{
				cashAllButton.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DevCommands.Commands.GiveCashAll);
				cashAllButton.OnClick = () => IssueOrder(world, DeveloperMode.Orders.GiveCashAll);
			}

			var growResourcesButton = widget.GetOrNull<ButtonWidget>("GROW_RESOURCES");
			if (growResourcesButton != null)
			{
				growResourcesButton.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DevCommands.Commands.GrowResources);
				growResourcesButton.OnClick = () => IssueOrder(world, DeveloperMode.Orders.GrowResources);
			}

			var disposeButton = widget.GetOrNull<ButtonWidget>("DISPOSE_SELECTED");
			if (disposeButton != null)
			{
				disposeButton.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DevCommands.Commands.Dispose);
				disposeButton.OnClick = () =>
				{
					foreach (var actor in world.Selection.Actors)
					{
						if (actor.Disposed)
							continue;

						world.IssueOrder(new Order(DeveloperMode.Orders.Dispose, world.LocalPlayer.PlayerActor, Target.FromActor(actor), false));
					}
				};
			}

			var killButton = widget.GetOrNull<ButtonWidget>("KILL_SELECTED");
			if (killButton != null)
			{
				killButton.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DevCommands.Commands.Kill);
				killButton.OnClick = () =>
				{
					foreach (var actor in world.Selection.Actors)
					{
						if (actor.IsDead)
							continue;

						world.IssueOrder(new Order(DeveloperMode.Orders.Kill, world.LocalPlayer.PlayerActor, Target.FromActor(actor), false) { TargetString = "" });
					}
				};
			}

			var fastBuildCheckbox = widget.GetOrNull<CheckboxWidget>("INSTANT_BUILD");
			if (fastBuildCheckbox != null)
			{
				fastBuildCheckbox.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DevCommands.Commands.FastBuild);
				BindOrderCheckbox(fastBuildCheckbox, world, DeveloperMode.Orders.FastBuild, () => devTrait.FastBuild);
			}

			var fastChargeCheckbox = widget.GetOrNull<CheckboxWidget>("INSTANT_CHARGE");
			if (fastChargeCheckbox != null)
			{
				fastChargeCheckbox.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DevCommands.Commands.FastCharge);
				BindOrderCheckbox(fastChargeCheckbox, world, DeveloperMode.Orders.FastCharge, () => devTrait.FastCharge);
			}

			var showCombatCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_COMBATOVERLAY");
			if (showCombatCheckbox != null)
			{
				showCombatCheckbox.GetTooltipText =
					() => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DebugVisualizationCommands.Commands.CombatGeometry);

				showCombatCheckbox.Disabled = debugVis == null || debugVisCommands == null;
				showCombatCheckbox.IsChecked = () => debugVis != null && debugVis.CombatGeometry;
				showCombatCheckbox.OnClick = () => debugVisCommands.InvokeCommand(DebugVisualizationCommands.Commands.CombatGeometry, "");
			}

			var showGeometryCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_GEOMETRY");
			if (showGeometryCheckbox != null)
			{
				showGeometryCheckbox.GetTooltipText =
					() => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DebugVisualizationCommands.Commands.RenderGeometry);

				showGeometryCheckbox.Disabled = debugVis == null || debugVisCommands == null;
				showGeometryCheckbox.IsChecked = () => debugVis != null && debugVis.RenderGeometry;
				showGeometryCheckbox.OnClick = () => debugVisCommands.InvokeCommand(DebugVisualizationCommands.Commands.RenderGeometry, "");
			}

			var showScreenMapCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_SCREENMAP");
			if (showScreenMapCheckbox != null)
			{
				showScreenMapCheckbox.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DebugVisualizationCommands.Commands.ScreenMap);
				showScreenMapCheckbox.Disabled = debugVis == null || debugVisCommands == null;
				showScreenMapCheckbox.IsChecked = () => debugVis != null && debugVis.ScreenMap;
				showScreenMapCheckbox.OnClick = () => debugVisCommands.InvokeCommand(DebugVisualizationCommands.Commands.ScreenMap, "");
			}

			var terrainGeometryTrait = world.WorldActor.TraitOrDefault<TerrainGeometryOverlay>();
			var showTerrainGeometryCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_TERRAIN_OVERLAY");
			if (showTerrainGeometryCheckbox != null && terrainGeometryTrait != null)
			{
				showTerrainGeometryCheckbox.GetTooltipText =
					() => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + TerrainGeometryOverlay.CommandName);

				showTerrainGeometryCheckbox.IsChecked = () => terrainGeometryTrait.Enabled;
				showTerrainGeometryCheckbox.OnClick = () => terrainGeometryTrait.InvokeCommand(TerrainGeometryOverlay.CommandName, "");
			}

			var showDepthPreviewCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_DEPTH_PREVIEW");
			if (showDepthPreviewCheckbox != null)
			{
				showDepthPreviewCheckbox.GetTooltipText =
					() => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DebugVisualizationCommands.Commands.DepthBuffer);

				showDepthPreviewCheckbox.Disabled = debugVis == null || debugVisCommands == null;
				showDepthPreviewCheckbox.IsChecked = () => debugVis != null && debugVis.DepthBuffer;
				showDepthPreviewCheckbox.OnClick = () => debugVisCommands.InvokeCommand(DebugVisualizationCommands.Commands.DepthBuffer, "");
			}

			var allTechCheckbox = widget.GetOrNull<CheckboxWidget>("ENABLE_TECH");
			if (allTechCheckbox != null)
			{
				allTechCheckbox.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DevCommands.Commands.EnableTech);
				BindOrderCheckbox(allTechCheckbox, world, DeveloperMode.Orders.EnableTech, () => devTrait.AllTech);
			}

			var powerCheckbox = widget.GetOrNull<CheckboxWidget>("UNLIMITED_POWER");
			if (powerCheckbox != null)
			{
				powerCheckbox.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DevCommands.Commands.UnlimitedPower);
				BindOrderCheckbox(powerCheckbox, world, DeveloperMode.Orders.UnlimitedPower, () => devTrait.UnlimitedPower);
			}

			var buildAnywhereCheckbox = widget.GetOrNull<CheckboxWidget>("BUILD_ANYWHERE");
			if (buildAnywhereCheckbox != null)
			{
				buildAnywhereCheckbox.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DevCommands.Commands.BuildAnywhere);
				BindOrderCheckbox(buildAnywhereCheckbox, world, DeveloperMode.Orders.BuildAnywhere, () => devTrait.BuildAnywhere);
			}

			var explorationButton = widget.GetOrNull<ButtonWidget>("GIVE_EXPLORATION");
			if (explorationButton != null)
			{
				explorationButton.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DevCommands.Commands.GiveExploration);
				explorationButton.OnClick = () => IssueOrder(world, DeveloperMode.Orders.GiveExploration);
			}

			var noexplorationButton = widget.GetOrNull<ButtonWidget>("RESET_EXPLORATION");
			if (noexplorationButton != null)
			{
				noexplorationButton.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DevCommands.Commands.ResetExploration);
				noexplorationButton.OnClick = () => IssueOrder(world, DeveloperMode.Orders.ResetExploration);
			}

			var powerOutageButton = widget.GetOrNull<ButtonWidget>("POWER_OUTAGE");
			if (powerOutageButton != null)
			{
				powerOutageButton.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + PowerManager.CommandName);
				powerOutageButton.OnClick = () =>
					world.IssueOrder(new Order(PowerManager.OrderName, world.LocalPlayer.PlayerActor, false) { ExtraData = 250 });
			}

			var levelUpButton = widget.GetOrNull<ButtonWidget>("LEVEL_UP");
			if (levelUpButton != null)
			{
				levelUpButton.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + GainsExperience.CommandName);
				levelUpButton.OnClick = () =>
				{
					foreach (var actor in world.Selection.Actors)
					{
						if (actor.IsDead || !actor.Info.HasTraitInfo<GainsExperienceInfo>())
							continue;

						world.IssueOrder(new Order(GainsExperience.OrderName, actor, false));
					}
				};
			}

			var playerExperienceButton = widget.GetOrNull<ButtonWidget>("PLAYER_EXPERIENCE");
			if (playerExperienceButton != null)
			{
				playerExperienceButton.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DevCommands.Commands.PlayerExperience);
				playerExperienceButton.OnClick = () =>
					world.IssueOrder(new Order(DeveloperMode.Orders.PlayerExperience, world.LocalPlayer.PlayerActor, false) { ExtraData = 1000 });
			}

			var showActorTagsCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_ACTOR_TAGS");
			if (showActorTagsCheckbox != null)
			{
				showActorTagsCheckbox.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + DebugVisualizationCommands.Commands.ActorTags);
				showActorTagsCheckbox.Disabled = debugVis == null || debugVisCommands == null;
				showActorTagsCheckbox.IsChecked = () => debugVis != null && debugVis.ActorTags;
				showActorTagsCheckbox.OnClick = () => debugVisCommands.InvokeCommand(DebugVisualizationCommands.Commands.ActorTags, "");
			}

			var showCustomTerrainCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_CUSTOMTERRAIN_OVERLAY");
			if (showCustomTerrainCheckbox != null)
			{
				showCustomTerrainCheckbox.GetTooltipText =
					() => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + CustomTerrainDebugOverlay.CommandName);

				var customTerrainDebugTrait = world.WorldActor.TraitOrDefault<CustomTerrainDebugOverlay>();
				showCustomTerrainCheckbox.Disabled = customTerrainDebugTrait == null;
				if (customTerrainDebugTrait != null)
				{
					showCustomTerrainCheckbox.IsChecked = () => customTerrainDebugTrait.Enabled;
					showCustomTerrainCheckbox.OnClick = () => customTerrainDebugTrait.InvokeCommand(CustomTerrainDebugOverlay.CommandName, "");
				}
			}

			var cellTriggerOverlayTrait = world.WorldActor.TraitOrDefault<CellTriggerOverlay>();
			var showCellTriggerOverlayCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_CELLTRIGGER_OVERLAY");
			if (showCellTriggerOverlayCheckbox != null)
			{
				showCellTriggerOverlayCheckbox.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + CellTriggerOverlay.CommandName);
				showCellTriggerOverlayCheckbox.Disabled = cellTriggerOverlayTrait == null;
				if (cellTriggerOverlayTrait != null)
				{
					showCellTriggerOverlayCheckbox.IsChecked = () => cellTriggerOverlayTrait.Enabled;
					showCellTriggerOverlayCheckbox.OnClick = () => cellTriggerOverlayTrait.InvokeCommand(CellTriggerOverlay.CommandName, "");
				}
			}

			var actorMapOverlayTrait = world.WorldActor.TraitOrDefault<ActorMapOverlay>();
			var showActorMapOverlayCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_ACTORMAP_OVERLAY");
			if (showActorMapOverlayCheckbox != null)
			{
				showActorMapOverlayCheckbox.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + ActorMapOverlay.CommandName);
				showActorMapOverlayCheckbox.Disabled = actorMapOverlayTrait == null;
				if (actorMapOverlayTrait != null)
				{
					showActorMapOverlayCheckbox.IsChecked = () => actorMapOverlayTrait.Enabled;
					showActorMapOverlayCheckbox.OnClick = () => actorMapOverlayTrait.InvokeCommand(ActorMapOverlay.CommandName, "");
				}
			}

			var hpfTrait = world.WorldActor.TraitOrDefault<HierarchicalPathFinderOverlay>();
			var showHpfOverlayCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_HPF_OVERLAY");
			if (showHpfOverlayCheckbox != null)
			{
				showHpfOverlayCheckbox.GetTooltipText = () => FluentProvider.GetMessage(TooltipDebugCommand, "command", '/' + HierarchicalPathFinderOverlay.CommandName);
				showHpfOverlayCheckbox.Disabled = hpfTrait == null;
				if (hpfTrait != null)
				{
					showHpfOverlayCheckbox.IsChecked = () => hpfTrait.Enabled;
					showHpfOverlayCheckbox.OnClick = () => hpfTrait.InvokeCommand(HierarchicalPathFinderOverlay.CommandName, "");
				}
			}

			var scrollPanel = widget.GetOrNull<ScrollPanelWidget>("DEBUG_SCROLLPANEL");
			scrollPanel?.Layout.AdjustChildren();
		}

		static void BindOrderCheckbox(CheckboxWidget checkbox, World world, string order, Func<bool> getValue)
		{
			var isChecked = new PredictedCachedTransform<bool, bool>(state => state);
			checkbox.IsChecked = () => isChecked.Update(getValue());
			checkbox.OnClick = () =>
			{
				isChecked.Predict(!getValue());
				IssueOrder(world, order);
			};
		}

		public static void IssueOrder(World world, string order)
		{
			world.IssueOrder(new Order(order, world.LocalPlayer.PlayerActor, false));
		}
	}
}
