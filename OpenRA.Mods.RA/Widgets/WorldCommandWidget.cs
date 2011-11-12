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
		public WorldCommandWidget([ObjectCreator.Param] OrderManager orderManager )
		{
			OrderManager = orderManager;
		}

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
				if (Game.Settings.Keys.InvertCtrlBehaviour ^ e.Modifiers.HasModifier(KeyConfig.HotkeyModifier))
				{
					if (KeyName == Rules.Info["mcv"].Traits.Get<BuildableInfo>().Hotkey)
						return CycleProductionBuildings("BaseType");

					if ((KeyName == Rules.Info["barr"].Traits.Get<BuildableInfo>().Hotkey)
						|| (KeyName == Rules.Info["tent"].Traits.Get<BuildableInfo>().Hotkey))
						return CycleProductionBuildings("BarracksType");

					if (KeyName == Rules.Info["weap"].Traits.Get<BuildableInfo>().Hotkey)
						return CycleProductionBuildings("WarFactoryType");

					if ((KeyName == Rules.Info["spen"].Traits.Get<BuildableInfo>().Hotkey)
						|| (KeyName == Rules.Info["syrd"].Traits.Get<BuildableInfo>().Hotkey))
						return CycleProductionBuildings("DockType");

					if ((KeyName == Rules.Info["hpad"].Traits.Get<BuildableInfo>().Hotkey)
						|| (KeyName == Rules.Info["afld"].Traits.Get<BuildableInfo>().Hotkey))
						return CycleProductionBuildings("AirportType");
				}

				if (!World.Selection.Actors.Any())	// Put all Cycle-functions before this line!
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
				"attackmove", MouseButton.Right);

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
				.Where(a => a.Owner == World.LocalPlayer)
				.Select(a => Pair.New( a, a.TraitOrDefault<AutoTarget>() ))
				.Where(a => a.Second != null).FirstOrDefault();

			if (actor.First == null)
				return true;

			var stances = (UnitStance[])Enum.GetValues(typeof(UnitStance));

			var nextStance = stances.Concat(stances).SkipWhile(s => s != actor.Second.stance).Skip(1).First();

			PerformKeyboardOrderOnSelection(a =>
				new Order("SetUnitStance", a, false) { TargetLocation = new int2((int)nextStance, 0) });

			Game.Debug( "Unit stance set to: {0}".F(nextStance) );

			return true;
		}

		bool CycleProductionBuildings(string DesiredBuildingType)
		{
			var buildings = World.ActorsWithTrait<ProductionBuilding>()
					.Where( a => (a.Actor.Owner == World.LocalPlayer)
							&& (a.Actor.Info.Traits.Get<ProductionBuildingInfo>().BuildingType == DesiredBuildingType)
												).ToArray();
			if (!buildings.Any()) return true;

			var next = buildings
				.Select(b => b.Actor)
				.SkipWhile(b => !World.Selection.Actors.Contains(b))
				.Skip(1)
				.FirstOrDefault();

			if (next == null)
				next = buildings.Select(b => b.Actor).First();

			World.Selection.Combine(World, new Actor[] { next }, false, true);
			Game.viewport.Center(World.Selection.Actors);

			return true;
		}

		bool unitsSelected()
		{
			if (World.Selection.Actors.Any( a => a.Owner == World.LocalPlayer && !a.HasTrait<Building>() )) return true;

			return false;
		}
	}
}