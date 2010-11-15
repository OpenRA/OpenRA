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
		public char HoldGroundKey = 'g'; // Hold (G)round
		public char DefensiveKey = 'd'; // (D)efensive
		public char AggressiveKey = 'a'; // (A)ggressive
		public char ReturnFireKey = 'r'; // (R)eturn Fire
		public char HoldFireKey = 'h'; // (h)old fire
		public readonly OrderManager OrderManager;

		[ObjectCreator.UseCtor]
		public WorldCommandWidget([ObjectCreator.Param] OrderManager orderManager )
		{
			OrderManager = orderManager;
		}

		public override void DrawInner(WorldRenderer wr)
		{
			
		}

		public override string GetCursor(int2 pos)
		{
			return null;
		}

		public override bool HandleKeyPressInner(KeyInput e)
		{
			if (World == null) return false;
			if (World.LocalPlayer == null) return false;

			return ProcessInput(e);
		}

		bool ProcessInput(KeyInput e)
		{
			// command: AttackMove
			if (e.KeyChar == AttackMoveKey && e.Modifiers == Modifiers.None)
			{
				return PerformAttackMove();
			}

/*			// command: GuardStance
			if (e.KeyChar == HoldGroundKey && (e.Modifiers.HasModifier(Modifiers.Alt)))
			{
				return EnableStance<UnitStanceHoldGround>();
			}

			// command: AggressiveStance
			if (e.KeyChar == AggressiveKey && (e.Modifiers.HasModifier(Modifiers.Alt)))
			{
				return EnableStance<UnitStanceAggressive>();
			}

			// stance: Return Fire
			// description: Fires only when fired upon, stops firing if no longer under attack
			if (e.KeyChar == ReturnFireKey && (e.Modifiers.HasModifier(Modifiers.Alt)))
			{
				return EnableStance<UnitStanceReturnFire>();
			}

			// stance: Hold Fire
			// description: Prevents attacking (ie no autotarget is being done)
			if (e.KeyChar == HoldFireKey && (e.Modifiers.HasModifier(Modifiers.Alt)))
			{
				return EnableStance<UnitStanceHoldFire>();
			}

			// stance: Defensive
			if (e.KeyChar == DefensiveKey && (e.Modifiers.HasModifier(Modifiers.Alt)))
			{
				return EnableStance<UnitStanceDefensive>();
			}	*/
			
			return false;
		}

		//bool EnableStance<T>() where T : UnitStance
		//{
		//    if (World.Selection.Actors.Count() == 0)
		//        return false;

		//    var traits =
		//        World.Selection.Actors.Where(a => !a.Destroyed && a.Owner == World.LocalPlayer && a.TraitOrDefault<T>() != null && !UnitStance.IsActive<T>(a)).
		//            Select(a => new Pair<Actor, T>(a, a.TraitOrDefault<T>()) );
			
		//    World.AddFrameEndTask(w => traits.Do(p => UnitStance.OrderStance(p.First, p.Second)));

		//    return traits.Any();
		//}

		bool PerformAttackMove()
		{
			if (World.Selection.Actors.Count() > 0)
			{
				World.OrderGenerator = new GenericSelectTarget(World.Selection.Actors, "AttackMove", "attackmove", MouseButton.Right);

				return true;
			}

			return false;
		}
	}
}