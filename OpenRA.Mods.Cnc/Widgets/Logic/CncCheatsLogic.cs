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
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncCheatsLogic
	{
		[ObjectCreator.UseCtor]
		public CncCheatsLogic(Widget widget, Action onExit, World world)
		{
			var panel = widget.GetWidget("CHEATS_PANEL");

			var devTrait = world.LocalPlayer.PlayerActor.Trait<DeveloperMode>();
			var shroudCheckbox = panel.GetWidget<CheckboxWidget>("SHROUD_CHECKBOX");
			shroudCheckbox.IsChecked = () => devTrait.DisableShroud;
			shroudCheckbox.OnClick = () => Order(world, "DevShroud");

			var pathCheckbox = panel.GetWidget<CheckboxWidget>("PATHDEBUG_CHECKBOX");
			pathCheckbox.IsChecked = () => devTrait.PathDebug;
			pathCheckbox.OnClick = () => Order(world, "DevPathDebug");


			panel.GetWidget<ButtonWidget>("GIVE_CASH_BUTTON").OnClick = () =>
				world.IssueOrder(new Order("DevGiveCash", world.LocalPlayer.PlayerActor, false));

			var fastBuildCheckbox = panel.GetWidget<CheckboxWidget>("INSTANT_BUILD_CHECKBOX");
			fastBuildCheckbox.IsChecked = () => devTrait.FastBuild;
			fastBuildCheckbox.OnClick = () => Order(world, "DevFastBuild");

			var fastChargeCheckbox = panel.GetWidget<CheckboxWidget>("INSTANT_CHARGE_CHECKBOX");
			fastChargeCheckbox.IsChecked = () => devTrait.FastCharge;
			fastChargeCheckbox.OnClick = () => Order(world, "DevFastCharge");

			var allTechCheckbox = panel.GetWidget<CheckboxWidget>("ENABLE_TECH_CHECKBOX");
			allTechCheckbox.IsChecked = () => devTrait.AllTech;
			allTechCheckbox.OnClick = () => Order(world, "DevEnableTech");

			var powerCheckbox = panel.GetWidget<CheckboxWidget>("UNLIMITED_POWER_CHECKBOX");
			powerCheckbox.IsChecked = () => devTrait.UnlimitedPower;
			powerCheckbox.OnClick = () => Order(world, "DevUnlimitedPower");

			var buildAnywhereCheckbox = panel.GetWidget<CheckboxWidget>("BUILD_ANYWHERE_CHECKBOX");
			buildAnywhereCheckbox.IsChecked = () => devTrait.BuildAnywhere;
			buildAnywhereCheckbox.OnClick = () => Order(world, "DevBuildAnywhere");

			panel.GetWidget<ButtonWidget>("GIVE_EXPLORATION_BUTTON").OnClick = () =>
				world.IssueOrder(new Order("DevGiveExploration", world.LocalPlayer.PlayerActor, false));

			panel.GetWidget<ButtonWidget>("CLOSE_BUTTON").OnClick = () => { Widget.CloseWindow(); onExit(); };
		}

		public void Order(World world, string order)
		{
			world.IssueOrder(new Order(order, world.LocalPlayer.PlayerActor, false));
		}
	}
}
