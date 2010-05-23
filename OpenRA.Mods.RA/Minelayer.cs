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

using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using System.Collections.Generic;
using System;

namespace OpenRA.Mods.RA
{
	class MinelayerInfo : TraitInfo<Minelayer>
	{
		public readonly string Mine = "minv";
		public readonly int MinefieldDepth = 2;
		public readonly string[] RearmBuildings = { "fix" };
	}

	class Minelayer : IIssueOrder, IResolveOrder
	{
		public int2[] minefield = null;
		int2 minefieldStart;		/* nosync! */

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Right && underCursor == null)
				return new Order("BeginMinefield", self, xy);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "BeginMinefield")
				if (self.Owner == self.World.LocalPlayer)
				{
					minefieldStart = order.TargetLocation;
					Game.controller.orderGenerator = new MinefieldOrderGenerator(self);
				}

			if (order.OrderString == "PlaceMinefield")
			{
				if (self.Owner == self.World.LocalPlayer)
					Game.controller.CancelInputMode();

				var movement = self.traits.Get<IMovement>();

				minefield = GetMinefieldCells(minefieldStart, order.TargetLocation,
					self.Info.Traits.Get<MinelayerInfo>().MinefieldDepth)
					.Where(p => movement.CanEnterCell(p)).ToArray();

				/* todo: start the mnly actually laying mines there */
			}
		}

		static IEnumerable<int2> GetMinefieldCells(int2 start, int2 end, int depth)
		{
			var mins = int2.Min(start, end);
			var maxs = int2.Max(start, end);

			/* todo: proper endcaps, if anyone cares (which won't happen unless depth is large) */

			var p = end - start;
			var q = new float2(p.Y, -p.X);
			q = (start != end) ? (1 / q.Length) * q : new float2(1, 0);
			var c = -float2.Dot(q, start);

			/* return all points such that |ax + by + c| < depth */

			for (var i = mins.X; i <= maxs.X; i++)
				for (var j = mins.Y; j <= maxs.Y; j++)
					if (Math.Abs(q.X * i + q.Y * j + c) < depth)
						yield return new int2(i, j);
		}

		class MinefieldOrderGenerator : IOrderGenerator
		{
			Actor minelayer;

			public MinefieldOrderGenerator(Actor self) { minelayer = self; }

			public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
			{
				var underCursor = world.FindUnitsAtMouse(mi.Location)
					.Where(a => a.Info.Traits.Contains<SelectableInfo>())
					.OrderByDescending(a => a.Info.Traits.Get<SelectableInfo>().Priority)
					.FirstOrDefault();

				if (mi.Button == MouseButton.Right && underCursor == null)
					yield return new Order("PlaceMinefield", minelayer, xy);
			}

			public void Tick(World world)
			{
				if (minelayer.IsDead || !minelayer.IsInWorld)
					Game.controller.CancelInputMode();
			}

			public void Render(World world) { }

			public string GetCursor(World world, int2 xy, MouseInput mi) { return "ability"; }	/* todo */
		}
	}
}
