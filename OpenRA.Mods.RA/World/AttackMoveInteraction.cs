using System.Linq;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class AttackMoveInteractionInfo : TraitInfo<AttackMoveInteraction>
	{

	}

	public class AttackMoveInteraction : INotifyKeyPress
	{
		public bool KeyPressed(Actor self, KeyInput e)
		{
			if (self.World.LocalPlayer == null) return false;

			if (e.KeyChar == 'a' && e.Modifiers == Modifiers.None)
			{
				StartAttackMoveOrder(self.World);
				return true;
			}

			return false;
		}

		public static void StartAttackMoveOrder(World world)
		{
			if (world.Selection.Actors.Count() > 0)
				world.OrderGenerator = new GenericSelectTarget(world.Selection.Actors, "AttackMove", "attackmove", MouseButton.Right);
		}
	}
}
