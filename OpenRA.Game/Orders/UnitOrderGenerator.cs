#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Support;

namespace OpenRA.Orders
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

		string ChooseCursor(World world, MouseInput mi)
		{
			using (new PerfSample("cursor"))
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
		}

		string CursorForOrderString(string s, Actor a, int2 location)
		{
			switch (s)
			{
				case "Attack": return "attack";
				case "Heal": return "heal";
				case "C4": return "c4";
				case "Move": 
					if (a.traits.GetOrDefault<IMovement>().CanEnterCell(location))
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
