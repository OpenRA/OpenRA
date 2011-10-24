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
			if (e.Modifiers == Modifiers.None && e.Event == KeyInputEvent.Down)
			{
				if (!World.Selection.Actors.Any())
					return false;

				if (e.KeyName == Game.Settings.KeyConfig.BaseCycleKey)
					return CycleBases();

				if (e.KeyName == Game.Settings.KeyConfig.AttackMoveKey)
					return PerformAttackMove();

				if (e.KeyName == Game.Settings.KeyConfig.StopKey)
					return PerformStop();

				if (e.KeyName == Game.Settings.KeyConfig.ScatterKey)
					return PerformScatter();

				if (e.KeyName == Game.Settings.KeyConfig.DeployKey)
					return PerformDeploy();

				if (e.KeyName == Game.Settings.KeyConfig.StanceCycleKey)
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