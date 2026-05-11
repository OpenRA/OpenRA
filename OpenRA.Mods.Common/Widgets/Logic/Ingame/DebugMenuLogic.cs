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
		[ObjectCreator.UseCtor]
		public DebugMenuLogic(Widget widget, World world)
		{
			var devTrait = world.LocalPlayer.PlayerActor.Trait<DeveloperMode>();
			var debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();
			var debugVisCommands = world.WorldActor.TraitOrDefault<DebugVisualizationCommands>();

			var visibilityCheckbox = widget.GetOrNull<CheckboxWidget>("DISABLE_VISIBILITY_CHECKS");
			if (visibilityCheckbox != null)
				BindOrderCheckbox(visibilityCheckbox, world, DeveloperMode.Orders.Visibility, () => devTrait.DisableShroud);

			var pathCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_UNIT_PATHS");
			if (pathCheckbox != null)
				BindOrderCheckbox(pathCheckbox, world, PathFinderOverlay.OrderName, () => devTrait.PathDebug);

			var cashButton = widget.GetOrNull<ButtonWidget>("GIVE_CASH");
			if (cashButton != null)
				cashButton.OnClick = () => IssueOrder(world, DeveloperMode.Orders.GiveCash);

			var growResourcesButton = widget.GetOrNull<ButtonWidget>("GROW_RESOURCES");
			if (growResourcesButton != null)
				growResourcesButton.OnClick = () => IssueOrder(world, DeveloperMode.Orders.GrowResources);

			var fastBuildCheckbox = widget.GetOrNull<CheckboxWidget>("INSTANT_BUILD");
			if (fastBuildCheckbox != null)
				BindOrderCheckbox(fastBuildCheckbox, world, DeveloperMode.Orders.FastBuild, () => devTrait.FastBuild);

			var fastChargeCheckbox = widget.GetOrNull<CheckboxWidget>("INSTANT_CHARGE");
			if (fastChargeCheckbox != null)
				BindOrderCheckbox(fastChargeCheckbox, world, DeveloperMode.Orders.FastCharge, () => devTrait.FastCharge);

			var showCombatCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_COMBATOVERLAY");
			if (showCombatCheckbox != null)
			{
				showCombatCheckbox.Disabled = debugVis == null || debugVisCommands == null;
				showCombatCheckbox.IsChecked = () => debugVis != null && debugVis.CombatGeometry;
				showCombatCheckbox.OnClick = () => debugVisCommands.InvokeCommand(DebugVisualizationCommands.Commands.CombatGeometry, "");
			}

			var showGeometryCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_GEOMETRY");
			if (showGeometryCheckbox != null)
			{
				showGeometryCheckbox.Disabled = debugVis == null || debugVisCommands == null;
				showGeometryCheckbox.IsChecked = () => debugVis != null && debugVis.RenderGeometry;
				showGeometryCheckbox.OnClick = () => debugVisCommands.InvokeCommand(DebugVisualizationCommands.Commands.RenderGeometry, "");
			}

			var showScreenMapCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_SCREENMAP");
			if (showScreenMapCheckbox != null)
			{
				showScreenMapCheckbox.Disabled = debugVis == null || debugVisCommands == null;
				showScreenMapCheckbox.IsChecked = () => debugVis != null && debugVis.ScreenMap;
				showScreenMapCheckbox.OnClick = () => debugVisCommands.InvokeCommand(DebugVisualizationCommands.Commands.ScreenMap, "");
			}

			var terrainGeometryTrait = world.WorldActor.TraitOrDefault<TerrainGeometryOverlay>();
			var showTerrainGeometryCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_TERRAIN_OVERLAY");
			if (showTerrainGeometryCheckbox != null && terrainGeometryTrait != null)
			{
				showTerrainGeometryCheckbox.IsChecked = () => terrainGeometryTrait.Enabled;
				showTerrainGeometryCheckbox.OnClick = () => terrainGeometryTrait.InvokeCommand(TerrainGeometryOverlay.CommandName, "");
			}

			var showDepthPreviewCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_DEPTH_PREVIEW");
			if (showDepthPreviewCheckbox != null)
			{
				showDepthPreviewCheckbox.Disabled = debugVis == null || debugVisCommands == null;
				showDepthPreviewCheckbox.IsChecked = () => debugVis != null && debugVis.DepthBuffer;
				showDepthPreviewCheckbox.OnClick = () => debugVisCommands.InvokeCommand(DebugVisualizationCommands.Commands.DepthBuffer, "");
			}

			var allTechCheckbox = widget.GetOrNull<CheckboxWidget>("ENABLE_TECH");
			if (allTechCheckbox != null)
				BindOrderCheckbox(allTechCheckbox, world, DeveloperMode.Orders.EnableTech, () => devTrait.AllTech);

			var powerCheckbox = widget.GetOrNull<CheckboxWidget>("UNLIMITED_POWER");
			if (powerCheckbox != null)
				BindOrderCheckbox(powerCheckbox, world, DeveloperMode.Orders.UnlimitedPower, () => devTrait.UnlimitedPower);

			var buildAnywhereCheckbox = widget.GetOrNull<CheckboxWidget>("BUILD_ANYWHERE");
			if (buildAnywhereCheckbox != null)
				BindOrderCheckbox(buildAnywhereCheckbox, world, DeveloperMode.Orders.BuildAnywhere, () => devTrait.BuildAnywhere);

			var explorationButton = widget.GetOrNull<ButtonWidget>("GIVE_EXPLORATION");
			if (explorationButton != null)
				explorationButton.OnClick = () => IssueOrder(world, DeveloperMode.Orders.GiveExploration);

			var noexplorationButton = widget.GetOrNull<ButtonWidget>("RESET_EXPLORATION");
			if (noexplorationButton != null)
				noexplorationButton.OnClick = () => IssueOrder(world, DeveloperMode.Orders.ResetExploration);

			var showActorTagsCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_ACTOR_TAGS");
			if (showActorTagsCheckbox != null)
			{
				showActorTagsCheckbox.Disabled = debugVis == null || debugVisCommands == null;
				showActorTagsCheckbox.IsChecked = () => debugVis != null && debugVis.ActorTags;
				showActorTagsCheckbox.OnClick = () => debugVisCommands.InvokeCommand(DebugVisualizationCommands.Commands.ActorTags, "");
			}

			var showCustomTerrainCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_CUSTOMTERRAIN_OVERLAY");
			if (showCustomTerrainCheckbox != null)
			{
				var customTerrainDebugTrait = world.WorldActor.TraitOrDefault<CustomTerrainDebugOverlay>();
				showCustomTerrainCheckbox.Disabled = customTerrainDebugTrait == null;
				if (customTerrainDebugTrait != null)
				{
					showCustomTerrainCheckbox.IsChecked = () => customTerrainDebugTrait.Enabled;
					showCustomTerrainCheckbox.OnClick = () => customTerrainDebugTrait.InvokeCommand(CustomTerrainDebugOverlay.CommandName, "");
				}
			}
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
