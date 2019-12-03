using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Orders
{
	class SellOrderGenerator : GlobalButtonOrderGenerator
	{
		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			return OrderInner(world, mi);
		}

		protected IEnumerable<Order> OrderInner(World world, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				// This one is regular function just like classical C&C. No dragging function for this.
				// Just keep in mind that some unselectable\fog-obscured things can be sell.
				// For example: walls
				var actor = world.ScreenMap.ActorsAtMouse(mi)
					.Select(a => a.Actor)
					.FirstOrDefault(a => a.Owner == world.LocalPlayer && a.AppearsFriendlyTo(world.LocalPlayer.PlayerActor)
						&& a.TraitsImplementing<Sellable>().Any(IsValidTrait));

				if (actor == null)
					yield break;

				yield return new Order("Sell", actor, false);
			}
		}

		protected bool IsValidTrait(Sellable t)
		{
			return Exts.IsTraitEnabled(t);
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var underCursor = world.ScreenMap.ActorsAtMouse(mi)
					.Select(a => a.Actor)
					.Where(a => a.Owner == world.LocalPlayer && a.AppearsFriendlyTo(world.LocalPlayer.PlayerActor)
						&& a.TraitsImplementing<Sellable>().Any(IsValidTrait));

			// This cursor is using regular rules just like classical C&C.
			// Just keep in mind that some unselectable\fog-obscured things can be sell.
			// For example: walls
			if (underCursor.Any())
				return "sell";
			else
				return "sell-blocked";
		}
	}
}
