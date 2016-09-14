#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Orders;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	/// <summary> Contains all functions that are unit-specific. </summary>
	public class UnitCommandWidget : Widget
	{
		readonly World world;

		[ObjectCreator.UseCtor]
		public UnitCommandWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
		}

		public override string GetCursor(int2 pos) { return null; }
		public override Rectangle GetEventBounds() { return Rectangle.Empty; }

		public override bool HandleKeyPress(KeyInput e)
		{
			if (world == null)
				return false;

			// Put all functions that are valid for observers/spectators inside WorldCommandWidget.
			if (world.LocalPlayer == null || world.LocalPlayer.Spectating)
				return false;

			if (world.IsGameOver || !world.Selection.Actors.Any())
				return false;

			return ProcessInput(e);
		}

		bool ProcessInput(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
			{
				var key = Hotkey.FromKeyInput(e);
				var ks = Game.Settings.Keys;

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

			if (actors.Any(a => a.Info.HasTraitInfo<AttackMoveInfo>() && a.Info.HasTraitInfo<AutoTargetInfo>()))
				world.OrderGenerator = new GenericSelectTarget(actors,
					"AttackMove", "attackmove", Game.Settings.Game.MouseButtonPreference.Action);

			return true;
		}

		void PerformKeyboardOrderOnSelection(Func<Actor, Order> f)
		{
			var orders = world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && !a.Disposed)
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
			PerformKeyboardOrderOnSelection(a => new Order("DeployToUpgrade", a, false));
			return true;
		}

		bool PerformStanceCycle()
		{
			var actor = world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && !a.Disposed)
				.Select(a => Pair.New(a, a.TraitOrDefault<AutoTarget>()))
				.FirstOrDefault(a => a.Second != null);

			if (actor.First == null)
				return true;

			var ati = actor.First.Info.TraitInfoOrDefault<AutoTargetInfo>();
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
				.Where(a => !a.Disposed && a.Owner == world.LocalPlayer);

			if (actors.Any(a => a.Info.HasTraitInfo<GuardInfo>() && a.Info.HasTraitInfo<AutoTargetInfo>()))
				world.OrderGenerator = new GuardOrderGenerator(actors,
					"Guard", "guard", Game.Settings.Game.MouseButtonPreference.Action);

			return true;
		}
	}
}
