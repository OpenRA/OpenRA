using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRa.Traits;
using OpenRa.GameRules;

namespace OpenRa.Orders
{
	class UnitOrderGenerator : IOrderGenerator
	{
		public readonly List<Actor> selection;

		public UnitOrderGenerator( IEnumerable<Actor> selected )
		{
			selection = selected.ToList();
		}

		public IEnumerable<Order> Order( World world, int2 xy, MouseInput mi )
		{
			foreach( var unit in selection )
			{
				var ret = unit.Order( xy, mi );
				if( ret != null )
					yield return ret;
			}
		}

		public void Tick( World world )
		{
			selection.RemoveAll(a => !a.IsInWorld);
		}

		public void Render( World world )
		{
			foreach( var a in selection )
				world.WorldRenderer.DrawSelectionBox( a, Color.White, true );
		}

		public Cursor GetCursor( World world, int2 xy, MouseInput mi )
		{
			return ChooseCursor(world, mi);
		}

		Cursor ChooseCursor( World world, MouseInput mi )
		{
			var p = Game.controller.MousePosition;
			var c = Order(world, p.ToInt2(), mi)
				.Select(o => CursorForOrderString(o.OrderString, o.Subject, o.TargetLocation))
				.FirstOrDefault(a => a != null);

			return c ??
				(world.SelectActorsInBox(Game.CellSize * p, 
				Game.CellSize * p).Any()
					? Cursor.Select : Cursor.Default);
		}

		Cursor CursorForOrderString(string s, Actor a, int2 location)
		{
			var movement = a.traits.GetOrDefault<IMovement>();
			switch (s)
			{
				case "Attack": return Cursor.Attack;
				case "Heal": return Cursor.Heal;
				case "C4": return Cursor.C4;
				case "Move":
					if (movement.CanEnterCell(location))
						return Cursor.Move;
					else
						return Cursor.MoveBlocked;
				case "DeployTransform":
					var depInfo = a.Info.Traits.Get<TransformsOnDeployInfo>();
					var transInfo = Rules.Info[depInfo.TransformsInto];
					if (transInfo.Traits.Contains<BuildingInfo>())
					{
						var bi = transInfo.Traits.Get<BuildingInfo>();
						if (!a.World.CanPlaceBuilding(depInfo.TransformsInto, bi, a.Location + new int2(depInfo.Offset[0], depInfo.Offset[1]), a))
							return Cursor.DeployBlocked;
					}
					return Cursor.Deploy;

				case "Deploy": return Cursor.Deploy;
				case "Enter": return Cursor.Enter;
				case "EnterTransport": return Cursor.Enter;
				case "Deliver": return Cursor.Enter;
				case "Infiltrate": return Cursor.Enter;
				case "Capture": return Cursor.Capture;
				case "Harvest": return Cursor.AttackMove;
				case "Steal" : return Cursor.Enter;
				default:
					return null;
			}
		}
	}
}
