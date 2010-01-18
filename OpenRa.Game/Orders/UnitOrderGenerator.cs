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

		public IEnumerable<Order> Order( int2 xy, MouseInput mi )
		{
			foreach( var unit in selection )
			{
				var ret = unit.Order( xy, mi );
				if( ret != null )
					yield return ret;
			}
		}

		public void Tick()
		{
			selection.RemoveAll(a => !a.IsInWorld);
		}

		public void Render()
		{
			foreach( var a in selection )
				Game.world.WorldRenderer.DrawSelectionBox( a, Color.White, true );
		}

		public Cursor GetCursor(int2 xy, MouseInput mi)
		{
			return ChooseCursor(mi);
		}

		Cursor ChooseCursor( MouseInput mi )
		{
			var p = Game.controller.MousePosition;
			var c = Order(p.ToInt2(), mi)
				.Where(o => o.Validate())
				.Select(o => CursorForOrderString(o.OrderString, o.Subject, o.TargetLocation))
				.FirstOrDefault(a => a != null);

			return c ??
				(Game.world.SelectActorsInBox(Game.CellSize * p, 
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
				case "DeployMcv":
					var factBuildingInfo = Rules.ActorInfo["fact"].Traits.Get<BuildingInfo>();
					if (Game.world.CanPlaceBuilding("fact", factBuildingInfo, a.Location - new int2(1, 1), a))
						return Cursor.Deploy;
					else
						return Cursor.DeployBlocked;
				case "Deploy": return Cursor.Deploy;
				case "Chronoshift":
					if (movement.CanEnterCell(location))
						return Cursor.Chronoshift;
					else
						return Cursor.MoveBlocked;
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
