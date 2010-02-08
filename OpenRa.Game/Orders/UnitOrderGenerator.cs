using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRa.Traits;
using OpenRa.GameRules;

namespace OpenRa.Orders
{
	class UnitOrderGenerator : IOrderGenerator
	{
		public IEnumerable<Order> Order( World world, int2 xy, MouseInput mi )
		{
			foreach( var unit in Game.controller.selection.Actors )
			{
				var ret = unit.Order( xy, mi );
				if( ret != null )
					yield return ret;
			}
		}

		public void Tick( World world ) {}

		public void Render( World world )
		{
			foreach( var a in Game.controller.selection.Actors )
				world.WorldRenderer.DrawSelectionBox( a, Color.White, true );
		}

		public string GetCursor( World world, int2 xy, MouseInput mi )
		{
			return ChooseCursor(world, mi);
		}

		string ChooseCursor( World world, MouseInput mi )
		{
			var p = Game.controller.MousePosition;
			var c = Order(world, p.ToInt2(), mi)
				.Select(o => CursorForOrderString(o.OrderString, o.Subject, o.TargetLocation))
				.FirstOrDefault(a => a != null);

			return c ??
				(world.SelectActorsInBox(Game.CellSize * p, 
				Game.CellSize * p).Any()
					? "select" : "default");
		}

		string CursorForOrderString(string s, Actor a, int2 location)
		{
			var movement = a.traits.GetOrDefault<IMovement>();
			switch (s)
			{
				case "Attack": return "attack";
				case "Heal": return "heal";
				case "C4": return "c4";
				case "Move":
					if (movement.CanEnterCell(location))
						return "move";
					else
						return "move-blocked";
				case "DeployTransform":
					var depInfo = a.Info.Traits.Get<TransformsOnDeployInfo>();
					var transInfo = Rules.Info[depInfo.TransformsInto];
					if (transInfo.Traits.Contains<BuildingInfo>())
					{
						var bi = transInfo.Traits.Get<BuildingInfo>();
						if (!a.World.CanPlaceBuilding(depInfo.TransformsInto, bi, a.Location + new int2(depInfo.Offset[0], depInfo.Offset[1]), a))
							return "deploy-blocked";
					}
					return "deploy";

				case "Deploy": return "deploy";
				case "Enter": return "enter";
				case "EnterTransport": return "enter";
				case "Deliver": return "enter";
				case "Infiltrate": return "enter";
				case "Capture": return "capture";
				case "Harvest": return "attackmove";
				case "Steal" : return "enter";
				default:
					return null;
			}
		}
	}
}
