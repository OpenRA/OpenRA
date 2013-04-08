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
using System.Drawing;
using System.Reflection;
using System.Linq;
using OpenRA;
using OpenRA.Traits;
using OpenRA.Widgets;
using XRandom = OpenRA.Thirdparty.Random;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class CheatsLogic
	{
		public static XRandom CosmeticRandom = new XRandom();

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

			var showMuzzlesCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_MUZZLES");
			if (showMuzzlesCheckbox != null)
			{
				showMuzzlesCheckbox.IsChecked = () => devTrait.ShowMuzzles;
				showMuzzlesCheckbox.OnClick = () => devTrait.ShowMuzzles ^= true;
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

			var dbgOverlay = world.WorldActor.TraitOrDefault<DebugOverlay>();
			var showAstarCostCheckbox = widget.GetOrNull<CheckboxWidget>("SHOW_ASTAR");
			if (showAstarCostCheckbox != null)
			{
				showAstarCostCheckbox.IsChecked = () => dbgOverlay != null ? dbgOverlay.Visible : false;
				showAstarCostCheckbox.OnClick = () => { if (dbgOverlay != null) dbgOverlay.Visible ^= true; };
			}

			var desync = widget.GetOrNull<ButtonWidget>("DESYNC");
			var desyncEnabled = widget.GetOrNull<CheckboxWidget>("DESYNC_ARMED");
			if (desync != null && desyncEnabled != null)
			{
				desyncEnabled.IsChecked = () => !desync.Disabled;
				desyncEnabled.OnClick = () => desync.Disabled ^= true;

				desync.Disabled = true;
				desync.OnClick = () => TriggerDesync(world);
			}

			var close = widget.GetOrNull<ButtonWidget>("CLOSE");
			if (close != null)
				close.OnClick = () => { Ui.CloseWindow(); onExit(); };
		}

		void TriggerDesync(World world)
		{
			var trait = world.ActorsWithTrait<ISync>().Random(CosmeticRandom).Trait;
			var t = trait.GetType();
			const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

			var fields = t.GetFields(bf).Where(x => x.HasAttribute<SyncAttribute>()).ToArray();
			if (fields.Length > 0)
			{
				var f = fields[CosmeticRandom.Next(fields.Length)];
				var before = f.GetValue(trait);

				if (f.FieldType == typeof(Boolean))
					f.SetValue(trait, !(Boolean) f.GetValue(trait));
				else if (f.FieldType == typeof(Int32))
					f.SetValue(trait, CosmeticRandom.Next(Int32.MaxValue));
				else
					Game.AddChatLine(Color.White, "Debug", "Sorry, Field-Type not implemented. Try again!");

				var after = f.GetValue(trait);
				Game.AddChatLine(Color.White, "Debug", "Type: {0}\nField: ({1}) {2}\nBefore: {3}\nAfter: {4}"
					.F(t.Name, f.FieldType.Name, f.Name, before, after));
			}
			else
				Game.AddChatLine(Color.White, "Debug", "Bad random choice. This trait has no fields. Try again!");
		}

		public void Order(World world, string order)
		{
			world.IssueOrder(new Order(order, world.LocalPlayer.PlayerActor, false));
		}
	}
}
