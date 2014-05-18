#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Support;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class CheatsLogic
	{
		public static MersenneTwister CosmeticRandom = new MersenneTwister();

		[ObjectCreator.UseCtor]
		public CheatsLogic(Widget widget, Action onExit, World world)
		{
			var devTrait = world.LocalPlayer.PlayerActor.Trait<DeveloperMode>();

			var shroudCheckbox = widget.GetOrNull<CheckboxWidget>("DISABLE_SHROUD");
			if (shroudCheckbox != null)
			{
				shroudCheckbox.IsChecked = () => devTrait.DisableShroud;
				shroudCheckbox.OnClick = () => Order(world, "DevShroudDisable");
			}

			var pathCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_UNIT_PATHS");
			if (pathCheckbox != null)
			{
				pathCheckbox.IsChecked = () => devTrait.PathDebug;
				pathCheckbox.OnClick = () => Order(world, "DevPathDebug");
			}

			var cashButton = widget.GetOrNull<ButtonWidget>("GIVE_CASH");
			if (cashButton != null)
				cashButton.OnClick = () =>
				world.IssueOrder(new Order("DevGiveCash", world.LocalPlayer.PlayerActor, false));

			var growResourcesButton = widget.GetOrNull<ButtonWidget>("GROW_RESOURCES");
			if (growResourcesButton != null)
				growResourcesButton.OnClick = () =>
				world.IssueOrder(new Order("DevGrowResources", world.LocalPlayer.PlayerActor, false));

			var fastBuildCheckbox = widget.GetOrNull<CheckboxWidget>("INSTANT_BUILD");
			if (fastBuildCheckbox != null)
			{
				fastBuildCheckbox.IsChecked = () => devTrait.FastBuild;
				fastBuildCheckbox.OnClick = () => Order(world, "DevFastBuild");
			}

			var fastChargeCheckbox = widget.GetOrNull<CheckboxWidget>("INSTANT_CHARGE");
			if (fastChargeCheckbox != null)
			{
				fastChargeCheckbox.IsChecked = () => devTrait.FastCharge;
				fastChargeCheckbox.OnClick = () => Order(world, "DevFastCharge");
			}

			var showCombatCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_COMBATOVERLAY");
			if (showCombatCheckbox != null)
			{
				showCombatCheckbox.IsChecked = () => devTrait.ShowCombatGeometry;
				showCombatCheckbox.OnClick = () => devTrait.ShowCombatGeometry ^= true;
			}

			var showGeometryCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_GEOMETRY");
			if (showGeometryCheckbox != null)
			{
				showGeometryCheckbox.IsChecked = () => devTrait.ShowDebugGeometry;
				showGeometryCheckbox.OnClick = () => devTrait.ShowDebugGeometry ^= true;
			}

			var allTechCheckbox = widget.GetOrNull<CheckboxWidget>("ENABLE_TECH");
			if (allTechCheckbox != null)
			{
				allTechCheckbox.IsChecked = () => devTrait.AllTech;
				allTechCheckbox.OnClick = () => Order(world, "DevEnableTech");
			}

			var powerCheckbox = widget.GetOrNull<CheckboxWidget>("UNLIMITED_POWER");
			if (powerCheckbox != null)
			{
				powerCheckbox.IsChecked = () => devTrait.UnlimitedPower;
				powerCheckbox.OnClick = () => Order(world, "DevUnlimitedPower");
			}

			var buildAnywhereCheckbox = widget.GetOrNull<CheckboxWidget>("BUILD_ANYWHERE");
			if (buildAnywhereCheckbox != null)
			{
				buildAnywhereCheckbox.IsChecked = () => devTrait.BuildAnywhere;
				buildAnywhereCheckbox.OnClick = () => Order(world, "DevBuildAnywhere");
			}

			var explorationButton = widget.GetOrNull<ButtonWidget>("GIVE_EXPLORATION");
			if (explorationButton != null)
				explorationButton.OnClick = () =>
				world.IssueOrder(new Order("DevGiveExploration", world.LocalPlayer.PlayerActor, false));

			var noexplorationButton = widget.GetOrNull<ButtonWidget>("RESET_EXPLORATION");
			if (noexplorationButton != null)
				noexplorationButton.OnClick = () =>
				world.IssueOrder(new Order("DevResetExploration", world.LocalPlayer.PlayerActor, false));

			var dbgOverlay = world.WorldActor.TraitOrDefault<PathfinderDebugOverlay>();
			var showAstarCostCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_ASTAR");
			if (showAstarCostCheckbox != null)
			{
				showAstarCostCheckbox.IsChecked = () => dbgOverlay != null ? dbgOverlay.Visible : false;
				showAstarCostCheckbox.OnClick = () => { if (dbgOverlay != null) dbgOverlay.Visible ^= true; };
			}

			var close = widget.GetOrNull<ButtonWidget>("CLOSE");
			if (close != null)
				close.OnClick = () => { Ui.CloseWindow(); onExit(); };
		}

		public void Order(World world, string order)
		{
			world.IssueOrder(new Order(order, world.LocalPlayer.PlayerActor, false));
		}
	}
}
