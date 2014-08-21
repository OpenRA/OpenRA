#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Orders;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class WorldCommandWidget : Widget
	{
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly RadarPings radarPings;

		[ObjectCreator.UseCtor]
		public WorldCommandWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			radarPings = world.WorldActor.TraitOrDefault<RadarPings>();
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
			if (e.Event == KeyInputEvent.Down)
			{
				var key = Hotkey.FromKeyInput(e);
				var ks = Game.Settings.Keys;

				if (key == ks.CycleBaseKey)
					return CycleBases();

				if (key == ks.CycleProductionBuildingsKey)
					return CycleProductionBuildings();

				if (key == ks.ToLastEventKey)
					return ToLastEvent();

				if (key == ks.ToSelectionKey)
					return ToSelection();


				// Put all functions that aren't unit-specific before this line!
				if (!world.Selection.Actors.Any())
					return false;

				if (key == ks.AttackMoveKey)
					return PerformAttackMove();

				if (key == ks.StopKey)
					return PerformStop();

				if (key == ks.ScatterKey)
					return PerformScatter();

				if (key == ks.DeployKey)
					return PerformDeploy();

				if (key == ks.StanceCycleKey)
					return PerformStanceCycle();

				if (key == ks.GuardKey)
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
			// HACK: multiple orders here
			PerformKeyboardOrderOnSelection(a => new Order("ReturnToBase", a, false));
			PerformKeyboardOrderOnSelection(a => new Order("DeployTransform", a, false));
			PerformKeyboardOrderOnSelection(a => new Order("Unload", a, false));
			PerformKeyboardOrderOnSelection(a => new Order("Detonate", a, false));
			return true;
		}

		bool PerformStanceCycle()
		{
			var actor = world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && !a.Destroyed)
				.Select(a => Pair.New(a, a.TraitOrDefault<AutoTarget>()))
				.FirstOrDefault(a => a.Second != null);

			if (actor.First == null)
				return true;

			var ati = actor.First.Info.Traits.GetOrDefault<AutoTargetInfo>();
			if (ati == null || !ati.EnableStances)
				return false;

			var stances = Enum<UnitStance>.GetValues();
			var nextStance = stances.Concat(stances)
				.SkipWhile(s => s != actor.Second.PredictedStance)
				.Skip(1)
				.First();

			PerformKeyboardOrderOnSelection(a =>
			{
				var at = a.TraitOrDefault<AutoTarget>();
				if (at != null)
					at.PredictedStance = nextStance;

				return new Order("SetUnitStance", a, false) { ExtraData = (uint)nextStance };
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

		bool CycleProductionBuildings()
		{
			var facilities = world.ActorsWithTrait<Production>()
				.Where(a => a.Actor.Owner == world.LocalPlayer && !a.Actor.HasTrait<BaseBuilding>())
				.OrderBy(f => f.Actor.Info.Traits.Get<ProductionInfo>().Produces.First())
				.ToArray();

			if (!facilities.Any())
				return true;

			var next = facilities
				.Select(b => b.Actor)
				.SkipWhile(b => !world.Selection.Actors.Contains(b))
				.Skip(1)
				.FirstOrDefault();

			if (next == null)
				next = facilities.Select(b => b.Actor).First();

			world.Selection.Combine(world, new Actor[] { next }, false, true);

			return ToSelection();
		}

		bool ToLastEvent()
		{
			if (radarPings == null || radarPings.LastPingPosition == null)
				return true;

			worldRenderer.Viewport.Center(radarPings.LastPingPosition.Value);
			return true;
		}

		bool ToSelection()
		{
			worldRenderer.Viewport.Center(world.Selection.Actors);
			return true;
		}
	}
}
