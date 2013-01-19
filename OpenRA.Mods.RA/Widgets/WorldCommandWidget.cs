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
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Orders;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class WorldCommandWidget : Widget
	{
		public World World { get { return OrderManager.world; } }

		public readonly OrderManager OrderManager;

		[ObjectCreator.UseCtor]
		public WorldCommandWidget(OrderManager orderManager) { OrderManager = orderManager; }

		public override string GetCursor(int2 pos) { return null; }
		public override Rectangle GetEventBounds() { return Rectangle.Empty; }

		public override bool HandleKeyPress(KeyInput e)
		{
			if (World == null) return false;
			if (World.LocalPlayer == null) return false;

			return ProcessInput(e);
		}

		bool ProcessInput(KeyInput e)
		{
			var KeyName = e.KeyName;
			var KeyConfig = Game.Settings.Keys;

			if (e.Event == KeyInputEvent.Down)
			{
				if ((e.Modifiers == KeyConfig.ModifierToCycle) || (e.MultiTapCount >= 2))
				{
					if (e.MultiTapCount == 2)
						World.Selection.Clear();

					if (KeyName == KeyConfig.BuildingsTabKey)
						return CycleProductionBuildings("BaseType", true);

					if (KeyName == KeyConfig.InfantryTabKey)
						return CycleProductionBuildings("BarracksType", true);

					if (KeyName == KeyConfig.VehicleTabKey)
						return CycleProductionBuildings("WarFactoryType", true);

					if (KeyName == KeyConfig.ShipTabKey)
						return CycleProductionBuildings("DockType", true);

					if (KeyName == KeyConfig.PlaneTabKey)
						return CycleProductionBuildings("AirportType", true);

					if (KeyName == KeyConfig.DefenseTabKey)
					{
						CycleProductionBuildings("BaseType", true);
						Ui.Root.Get<BuildPaletteWidget>("INGAME_BUILD_PALETTE")
							.SetCurrentTab(World.LocalPlayer.PlayerActor.TraitsImplementing<ProductionQueue>()
								.FirstOrDefault( q => q.Info.Type == "Defense" ));
						return true;
					}
				}

				if ((e.Modifiers == KeyConfig.ModifierToSelectTab) && (e.MultiTapCount == 1))
				{
					if (KeyName == KeyConfig.BuildingsTabKey)
						return CycleProductionBuildings("BaseType", false);

					if (KeyName == KeyConfig.InfantryTabKey)
						return CycleProductionBuildings("BarracksType", false);

					if (KeyName == KeyConfig.VehicleTabKey)
						return CycleProductionBuildings("WarFactoryType", false);

					if (KeyName == KeyConfig.ShipTabKey)
						return CycleProductionBuildings("DockType", false);

					if (KeyName == KeyConfig.PlaneTabKey)
						return CycleProductionBuildings("AirportType", false);

					if (KeyName == KeyConfig.DefenseTabKey)
					{
						CycleProductionBuildings("BaseType", false);
						Ui.Root.Get<BuildPaletteWidget>("INGAME_BUILD_PALETTE")
							.SetCurrentTab(World.LocalPlayer.PlayerActor.TraitsImplementing<ProductionQueue>()
								.FirstOrDefault( q => q.Info.Type == "Defense" ));
						return true;
					}
				}

				if (KeyName == KeyConfig.FocusBaseKey)
					return CycleProductionBuildings("BaseType", true);

				if (KeyName == KeyConfig.FocusLastEventKey)
					return GotoLastEvent();

				if (KeyName == KeyConfig.SellKey)
					return PerformSwitchToSellMode();

				if (KeyName == KeyConfig.PowerDownKey)
					return PerformSwitchToPowerDownMode();

				if (KeyName == KeyConfig.RepairKey)
					return PerformSwitchToRepairMode();

				if (KeyName == KeyConfig.PlaceNormalBuildingKey)
					return PerformPlaceNormalBuilding();

				if (KeyName == KeyConfig.PlaceDefenseBuildingKey)
					return PerformPlaceDefenseBuilding();

				if (!World.Selection.Actors.Any())	// Put all functions, that are no unit-functions, before this line!
					return false;

				if ((KeyName == KeyConfig.AttackMoveKey) && unitsSelected())
					return PerformAttackMove();

				if ((KeyName == KeyConfig.StopKey) && unitsSelected())
					return PerformStop();

				if ((KeyName == KeyConfig.ScatterKey) && unitsSelected())
					return PerformScatter();

				if (KeyName == KeyConfig.DeployKey)
					return PerformDeploy();

				if ((KeyName == KeyConfig.StanceCycleKey) && unitsSelected())
					return PerformStanceCycle();
			}

			return false;
		}

		// todo: take ALL this garbage and route it through the OrderTargeter stuff.

		bool PerformAttackMove()
		{
			var actors = World.Selection.Actors
				.Where(a => a.Owner == World.LocalPlayer).ToArray();

			if (actors.Length > 0)
				World.OrderGenerator = new GenericSelectTarget(actors, "AttackMove",
				                                               "attackmove", Game.mouseButtonPreference.Action);

			return true;
		}

		void PerformKeyboardOrderOnSelection(Func<Actor, Order> f)
		{
			var orders = World.Selection.Actors
				.Where(a => a.Owner == World.LocalPlayer).Select(f).ToArray();
			foreach (var o in orders) World.IssueOrder(o);
			World.PlayVoiceForOrders(orders);
		}

		bool PerformStop()
		{
			PerformKeyboardOrderOnSelection(a => new Order("Stop", a, false));
			return true;
		}

		bool PerformScatter()
		{
			PerformKeyboardOrderOnSelection(a => new Order("Scatter", a, false));
			return true;
		}

		bool PerformDeploy()
		{
			/* hack: three orders here -- ReturnToBase, DeployTransform, Unload. */
			PerformKeyboardOrderOnSelection(a => new Order("ReturnToBase", a, false));
			PerformKeyboardOrderOnSelection(a => new Order("DeployTransform", a, false));
			PerformKeyboardOrderOnSelection(a => new Order("Unload", a, false));
			return true;
		}

		bool PerformStanceCycle()
		{
			var actor = World.Selection.Actors
				.Where(a => a.Owner == World.LocalPlayer && !a.Destroyed)
				.Select(a => Pair.New( a, a.TraitOrDefault<AutoTarget>() ))
				.Where(a => a.Second != null).FirstOrDefault();

			if (actor.First == null)
				return true;

			var stances = Enum<UnitStance>.GetValues();

			var nextStance = stances.Concat(stances).SkipWhile(s => s != actor.Second.predictedStance).Skip(1).First();

			PerformKeyboardOrderOnSelection(a =>
			{
				var at = a.TraitOrDefault<AutoTarget>();
				if (at != null) at.predictedStance = nextStance;
				// NOTE(jsd): Abuse of the type system here with `CPos`
				return new Order("SetUnitStance", a, false) { TargetLocation = new CPos((int)nextStance, 0) };
			});

			Game.Debug( "Unit stance set to: {0}".F(nextStance) );

			return true;
		}

		bool CycleProductionBuildings(string DesiredBuildingType, bool ChangeViewport)
		{
			var buildings = World.ActorsWithTrait<ProductionBuilding>()
					.Where(a => a.Actor.Owner ==
				       World.LocalPlayer && a.Actor.Info.Traits.Get<ProductionBuildingInfo>().BuildingType == DesiredBuildingType)
					.OrderByDescending(a => a.Actor.IsPrimaryBuilding()).ToArray();

			if (!buildings.Any()) return true;

			var next = buildings
				.Select(b => b.Actor)
				.SkipWhile(b => !World.Selection.Actors.Contains(b))
				.Skip(1)
				.FirstOrDefault();

			if (next == null)
				next = buildings.Select(b => b.Actor).First();

			World.Selection.Combine(World, new Actor[] { next }, false, true);
			if (ChangeViewport)
				Game.viewport.Center(World.Selection.Actors);

			return true;
		}

		bool GotoLastEvent()
		{
			if (World.LocalPlayer == null)
				return true;

			var eventNotifier = World.LocalPlayer.PlayerActor.TraitOrDefault<BaseAttackNotifier>();
			if (eventNotifier == null)
				return true;

			if (eventNotifier.lastAttackTime < 0)
				return true;

			Game.viewport.Center(eventNotifier.lastAttackLocation.ToFloat2());
			return true;
		}

		private bool PerformSwitchToSellMode()
		{
			World.ToggleInputMode<SellOrderGenerator>();
			return true;
		}

		private bool PerformSwitchToPowerDownMode()
		{
			World.ToggleInputMode<PowerDownOrderGenerator>();
			return true;
		}

		private bool PerformSwitchToRepairMode()
		{
			World.ToggleInputMode<RepairOrderGenerator>();
			return true;
		}

		private bool PerformPlaceNormalBuilding()
		{
			return SwitchToTab(0);
		}

		private bool PerformPlaceDefenseBuilding()
		{
			return SwitchToTab(1);
		}

		bool unitsSelected()
		{
			if (World.Selection.Actors.Any( a => a.Owner == World.LocalPlayer && !a.HasTrait<Building>() )) return true;

			return false;
		}

		private bool SwitchToTab(int num)
		{
			var types = World.Actors.Where(a => a.IsInWorld && (a.World.LocalPlayer == a.Owner))
								  .SelectMany(a => a.TraitsImplementing<Production>())
								  .SelectMany(t => t.Info.Produces)
								  .ToArray();

			if (types.Length == 0)
				return false;
			var tabs = World.LocalPlayer.PlayerActor.TraitsImplementing<ProductionQueue>().Where(t => types.Contains(t.Info.Type)).ToArray();
			if (tabs.Length <= num)
				return false;

			var tab = tabs[num];
			Ui.Root.Get<BuildPaletteWidget>("INGAME_BUILD_PALETTE")
				.SetCurrentTab(tab);

			if ((tab.Queue.Count() > 0) && (tab.CurrentDone))
			{
				if (Rules.Info[tab.CurrentItem().Item].Traits.Contains<BuildingInfo>())
					World.OrderGenerator = new PlaceBuildingOrderGenerator(tab.self, tab.CurrentItem().Item);
			}
			return true;
		}
	}
}
