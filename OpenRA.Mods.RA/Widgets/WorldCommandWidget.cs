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
			if (!World.Selection.Actors.Any())
				return false;

			if (e.Modifiers == Modifiers.None)
			{
				if (e.KeyChar == AttackMoveKey)
					return PerformAttackMove();

				if (e.KeyChar == StopKey)
					return PerformStop();
			}
			
			return false;
		}

		bool PerformAttackMove()
		{
			World.OrderGenerator = new GenericSelectTarget(World.Selection.Actors, "AttackMove", 
				"attackmove", MouseButton.Right);

			return true;
		}

		bool PerformStop()
		{
			/* issue a stop order to everyone. */
			foreach (var a in World.Selection.Actors)
				World.IssueOrder(new Order("Stop", a, false));

			return true;
		}
	}
}