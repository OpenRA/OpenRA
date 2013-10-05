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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Orders;
using OpenRA.Network;
using OpenRA.Orders;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class WorldCommandWidget : Widget
	{
		readonly World world;
		readonly WorldRenderer worldRenderer;

		[ObjectCreator.UseCtor]
		public WorldCommandWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
		}

		public override string GetCursor(int2 pos) { return null; }
		public override Rectangle GetEventBounds() { return Rectangle.Empty; }

		public override bool HandleKeyPress(KeyInput e)
		{
			if (world == null || world.LocalPlayer == null)
				return false;

			return ProcessInput(e);
		}

		bool ProcessInput(KeyInput e)
		{
			if (e.Modifiers == Modifiers.None && e.Event == KeyInputEvent.Down)
			{
				if (e.KeyName == Game.Settings.Keys.CycleBaseKey)
					return CycleBases();

				if (e.KeyName == Game.Settings.Keys.ToLastEventKey)
					return ToLastEvent();

				if (e.KeyName == Game.Settings.Keys.ToSelectionKey)
					return ToSelection();

				// Put all functions that aren't unit-specific before this line!
				if (!world.Selection.Actors.Any()) 
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

				if (e.KeyName == Game.Settings.Keys.GuardKey)
					return PerformGuard();
			}

			return false;
		}

		// TODO: take ALL this garbage and route it through the OrderTargeter stuff.
		bool PerformAttackMove()
		{
			var actors = world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer)
				.ToArray();

			if (actors.Any())
				world.OrderGenerator = new GenericSelectTarget(actors,
					"AttackMove", "attackmove", Game.mouseButtonPreference.Action);

			return true;
		}

		void PerformKeyboardOrderOnSelection(Func<Actor, Order> f)
		{
			var orders = world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && !a.Destroyed)
				.Select(f)
				.ToArray();

			foreach (var o in orders)
				world.IssueOrder(o);

			world.PlayVoiceForOrders(orders);
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
			var actor = world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && !a.Destroyed)
				.Select(a => Pair.New(a, a.TraitOrDefault<AutoTarget>()))
				.Where(a => a.Second != null)
				.FirstOrDefault();

			if (actor.First == null)
				return true;

			var stances = Enum<UnitStance>.GetValues();
			var nextStance = stances.Concat(stances)
				.SkipWhile(s => s != actor.Second.predictedStance)
				.Skip(1)
				.First();

			PerformKeyboardOrderOnSelection(a =>
			{
				var at = a.TraitOrDefault<AutoTarget>();
				if (at != null)
					at.predictedStance = nextStance;

				// FIXME: Abuse of the type system here with `CPos`
				return new Order("SetUnitStance", a, false) { TargetLocation = new CPos((int)nextStance, 0) };
			});

			Game.Debug("Unit stance set to: {0}".F(nextStance));

			return true;
		}

		bool PerformGuard()
		{
			var actors = world.Selection.Actors
				.Where(a => !a.Destroyed && a.Owner == world.LocalPlayer && a.HasTrait<Guard>());

			if (actors.Any())
				world.OrderGenerator = new GuardOrderGenerator(actors);

			return true;
		}

		bool CycleBases()
		{
			var bases = world.ActorsWithTrait<BaseBuilding>()
				.Where(a => a.Actor.Owner == world.LocalPlayer)
				.ToArray();

			if (!bases.Any())
				return true;

			var next = bases
				.Select(b => b.Actor)
				.SkipWhile(b => !world.Selection.Actors.Contains(b))
				.Skip(1)
				.FirstOrDefault();

			if (next == null)
				next = bases.Select(b => b.Actor).First();

			world.Selection.Combine(world, new Actor[] { next }, false, true);

			return ToSelection();
		}

		bool ToLastEvent()
		{
			if (world.LocalPlayer == null)
				return true;

			var eventNotifier = world.LocalPlayer.PlayerActor.TraitOrDefault<BaseAttackNotifier>();
			if (eventNotifier == null)
				return true;

			if (eventNotifier.lastAttackTime < 0)
				return true;

			worldRenderer.Viewport.Center(eventNotifier.lastAttackLocation.CenterPosition);
			return true;
		}

		bool ToSelection()
		{
			worldRenderer.Viewport.Center(world.Selection.Actors);
			return true;
		}
	}
}
