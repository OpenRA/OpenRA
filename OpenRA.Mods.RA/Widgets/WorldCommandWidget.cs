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

namespace OpenRA.Mods.RA.Widgets
{
	public class WorldCommandWidget : Widget
	{
		public World World { get { return OrderManager.world; } }

		public string AttackMoveKey = "a";
		public string StopKey = "s";
		public string ScatterKey = "x";
		public string DeployKey = "f";
		public string StanceCycleKey = "z";
		public string BaseCycleKey = "backspace";
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
				if (e.KeyName == BaseCycleKey)
					return CycleBases();

				if (!World.Selection.Actors.Any())
					return false;

				if (e.KeyName == AttackMoveKey)
					return PerformAttackMove();

				if (e.KeyName == StopKey)
					return PerformStop();

				if (e.KeyName == ScatterKey)
					return PerformScatter();

				if (e.KeyName == DeployKey)
					return PerformDeploy();

				if (e.KeyName == StanceCycleKey)
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

			var nextStance = stances.Concat(stances).SkipWhile(s => s != actor.Second.predictedStance).Skip(1).First();

			PerformKeyboardOrderOnSelection(a =>
			{
				var at = a.TraitOrDefault<AutoTarget>();
				if (at != null) at.predictedStance = nextStance;
				return new Order("SetUnitStance", a, false) { TargetLocation = new int2((int)nextStance, 0) };
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
	}
}