#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class CheatsLogic
	{
		[ObjectCreator.UseCtor]
		public CheatsLogic(Widget widget, Action onExit, World world)
		{
			var devTrait = world.LocalPlayer.PlayerActor.Trait<DeveloperMode>();

			var shroudCheckbox = widget.Get<CheckboxWidget>("DISABLE_SHROUD");
			shroudCheckbox.IsChecked = () => devTrait.DisableShroud;
			shroudCheckbox.OnClick = () => Order(world, "DevShroudDisable");

			var pathCheckbox = widget.Get<CheckboxWidget>("SHOW_UNIT_PATHS");
			pathCheckbox.IsChecked = () => devTrait.PathDebug;
			pathCheckbox.OnClick = () => Order(world, "DevPathDebug");

			widget.Get<ButtonWidget>("GIVE_CASH").OnClick = () =>
				world.IssueOrder(new Order("DevGiveCash", world.LocalPlayer.PlayerActor, false));

			var fastBuildCheckbox = widget.Get<CheckboxWidget>("INSTANT_BUILD");
			fastBuildCheckbox.IsChecked = () => devTrait.FastBuild;
			fastBuildCheckbox.OnClick = () => Order(world, "DevFastBuild");

			var fastChargeCheckbox = widget.Get<CheckboxWidget>("INSTANT_CHARGE");
			fastChargeCheckbox.IsChecked = () => devTrait.FastCharge;
			fastChargeCheckbox.OnClick = () => Order(world, "DevFastCharge");

			var showMuzzlesCheckbox = widget.Get<CheckboxWidget>("SHOW_MUZZLES");
			showMuzzlesCheckbox.IsChecked = () => devTrait.ShowMuzzles;
			showMuzzlesCheckbox.OnClick = () => devTrait.ShowMuzzles ^= true;

			var allTechCheckbox = widget.Get<CheckboxWidget>("ENABLE_TECH");
			allTechCheckbox.IsChecked = () => devTrait.AllTech;
			allTechCheckbox.OnClick = () => Order(world, "DevEnableTech");

			var powerCheckbox = widget.Get<CheckboxWidget>("UNLIMITED_POWER");
			powerCheckbox.IsChecked = () => devTrait.UnlimitedPower;
			powerCheckbox.OnClick = () => Order(world, "DevUnlimitedPower");

			var buildAnywhereCheckbox = widget.Get<CheckboxWidget>("BUILD_ANYWHERE");
			buildAnywhereCheckbox.IsChecked = () => devTrait.BuildAnywhere;
			buildAnywhereCheckbox.OnClick = () => Order(world, "DevBuildAnywhere");

			widget.Get<ButtonWidget>("GIVE_EXPLORATION").OnClick = () =>
				world.IssueOrder(new Order("DevGiveExploration", world.LocalPlayer.PlayerActor, false));

			widget.Get<ButtonWidget>("RESET_EXPLORATION").OnClick = () =>
				world.IssueOrder(new Order("DevResetExploration", world.LocalPlayer.PlayerActor, false));

			var dbgOverlay = world.WorldActor.TraitOrDefault<DebugOverlay>();
			var showAstarCostCheckbox = widget.Get<CheckboxWidget>("SHOW_ASTAR");
			showAstarCostCheckbox.IsChecked = () => dbgOverlay != null ? dbgOverlay.Visible : false;
			showAstarCostCheckbox.OnClick = () => { if (dbgOverlay != null) dbgOverlay.Visible ^= true; };

			widget.Get<ButtonWidget>("CLOSE").OnClick = () => { Ui.CloseWindow(); onExit(); };
		}

		public void Order(World world, string order)
		{
			world.IssueOrder(new Order(order, world.LocalPlayer.PlayerActor, false));
		}
	}
}
