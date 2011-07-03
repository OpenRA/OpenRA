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
		public string BaseCycleKey = "backspace";
		public readonly OrderManager OrderManager;

		[ObjectCreator.UseCtor]
		public WorldCommandWidget([ObjectCreator.Param] OrderManager orderManager )
		{
			OrderManager = orderManager;
		}

		public override string GetCursor(int2 pos) { return null; }

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
			}
			
			return false;
		}

		// todo: take ALL this garbage and route it through the OrderTargeter stuff.

		bool PerformAttackMove()
		{
			World.OrderGenerator = new GenericSelectTarget(World.Selection.Actors, "AttackMove", 
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
            /* hack: two orders here -- DeployTransform and Unload. */
			PerformKeyboardOrderOnSelection(a => new Order("DeployTransform", a, false));
            PerformKeyboardOrderOnSelection(a => new Order("Unload", a, false));
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