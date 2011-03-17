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

		public char AttackMoveKey = 'a';
		public char StopKey = 's';
		public char ScatterKey = 'x';
		public char DeployKey = 'f';
		public readonly OrderManager OrderManager;

		[ObjectCreator.UseCtor]
		public WorldCommandWidget([ObjectCreator.Param] OrderManager orderManager )
		{
			OrderManager = orderManager;
		}

		public override void DrawInner() { }

		public override string GetCursor(int2 pos) { return null; }

		public override bool HandleKeyPressInner(KeyInput e)
		{
			if (World == null) return false;
			if (World.LocalPlayer == null) return false;

			return ProcessInput(e);
		}

		bool ProcessInput(KeyInput e)
		{
			if (e.Modifiers == Modifiers.None)
			{
				if (e.KeyChar == '\b' || e.KeyChar == (char)127)
                    return CycleBases();
				
				if (!World.Selection.Actors.Any())
					return false;
				
            	if (e.KeyChar == AttackMoveKey)
					return PerformAttackMove();

				if (e.KeyChar == StopKey)
					return PerformStop();
				
				if (e.KeyChar == ScatterKey)
					return PerformScatter();

				if (e.KeyChar == DeployKey)
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
			var orders = World.Selection.Actors.Select(f).ToArray();
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