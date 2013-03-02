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
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Orders;
using OpenRA.Widgets;
using OpenRA.Mods.RA.Orders;

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
			if (e.Modifiers == Modifiers.None && e.Event == KeyInputEvent.Down)
			{
				if (e.KeyName == Game.Settings.Keys.CycleBaseKey)
					return CycleBases();

				if (e.KeyName == Game.Settings.Keys.GotoLastEventKey)
					return GotoLastEvent();

				if (e.KeyName == Game.Settings.Keys.SellKey)
					return PerformSwitchToSellMode();

				if (e.KeyName == Game.Settings.Keys.PowerDownKey)
					return PerformSwitchToPowerDownMode();

				if (e.KeyName == Game.Settings.Keys.RepairKey)
					return PerformSwitchToRepairMode();

				if (!World.Selection.Actors.Any()) // Put all functions, that are no unit-functions, before this line!
					return false;

				if (e.KeyName == Game.Settings.Keys.AttackMoveKey)
					return PerformAttackMove();

				if (e.KeyName == Game.Settings.Keys.StopKey)
					return PerformStop();

				if (e.KeyName == Game.Settings.Keys.ScatterKey)
					return PerformScatter();

				if (e.KeyName == Game.Settings.Keys.DeployKey)
					return PerformDeploy();

				if (e.KeyName == Game.Settings.Keys.StanceCycleKey)
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
			/* hack: multiple orders here */
			PerformKeyboardOrderOnSelection(a => new Order("ReturnToBase", a, false));
			PerformKeyboardOrderOnSelection(a => new Order("DeployTransform", a, false));
			PerformKeyboardOrderOnSelection(a => new Order("Unload", a, false));
			PerformKeyboardOrderOnSelection(a => new Order("DemoDeploy", a, false));
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

		bool CycleBases()
		{
			var bases = World.ActorsWithTrait<BaseBuilding>()
				.Where( a => a.Actor.Owner == World.LocalPlayer ).ToArray();
			if (!bases.Any()) return true;

			var next = bases
				.Select(b => b.Actor)
				.SkipWhile(b => !World.Selection.Actors.Contains(b))
				.Skip(1)
				.FirstOrDefault();

			if (next == null)
				next = bases.Select(b => b.Actor).First();

			World.Selection.Combine(World, new Actor[] { next }, false, true);
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

		bool PerformSwitchToSellMode()
		{
			World.ToggleInputMode<SellOrderGenerator>();
			return true;
		}

		bool PerformSwitchToPowerDownMode()
		{
			World.ToggleInputMode<PowerDownOrderGenerator>();
			return true;
		}

		bool PerformSwitchToRepairMode()
		{
			World.ToggleInputMode<RepairOrderGenerator>();
			return true;
		}
	}
}
