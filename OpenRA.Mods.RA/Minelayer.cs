#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class MinelayerInfo : TraitInfo<Minelayer>
	{
		[ActorReference]
		public readonly string Mine = "minv";
		public readonly float MinefieldDepth = 1.5f;
		[ActorReference]
		public readonly string[] RearmBuildings = { "fix" };
	}

	class Minelayer : IIssueOrder, IResolveOrder, IOrderCursor, IPostRenderSelection
	{
		/* [Sync] when sync can cope with arrays! */ 
		public int2[] minefield = null;
		[Sync] int2 minefieldStart;

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Right && underCursor == null && mi.Modifiers.HasModifier(Modifiers.Ctrl))
				return new Order("BeginMinefield", self, xy);

			return null;
		}

		public string CursorForOrder(Actor self, Order order)
		{
			return (order.OrderString == "BeginMinefield") ? "ability" : null;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "BeginMinefield")
			{
				minefieldStart = order.TargetLocation;
				if (self.Owner == self.World.LocalPlayer)
					self.World.OrderGenerator = new MinefieldOrderGenerator(self);
			}

			if (order.OrderString == "PlaceMinefield")
			{
				if (self.Owner == self.World.LocalPlayer)
					self.World.CancelInputMode();

				var movement = self.Trait<IMove>();

				minefield = GetMinefieldCells(minefieldStart, order.TargetLocation,
					self.Info.Traits.Get<MinelayerInfo>().MinefieldDepth)
					.Where(p => movement.CanEnterCell(p)).ToArray();

				self.CancelActivity();
				self.QueueActivity(new LayMines());
			}
		}

		static IEnumerable<int2> GetMinefieldCells(int2 start, int2 end, float depth)
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
				if (mi.Button == MouseButton.Left)
				{
					world.CancelInputMode();
					yield break;
				}

				var underCursor = world.FindUnitsAtMouse(mi.Location)
					//.Where(a => a.Info.Traits.Contains<SelectableInfo>())
					.OrderByDescending(a => a.Info.Traits.Contains<SelectableInfo>() ? a.Info.Traits.Get<SelectableInfo>().Priority : int.MinValue)
					.FirstOrDefault();

				if (mi.Button == MouseButton.Right && underCursor == null)
					yield return new Order("PlaceMinefield", minelayer, xy);
			}

			public void Tick(World world)
			{
				if (minelayer.IsDead() || !minelayer.IsInWorld)
					world.CancelInputMode();
			}

			int2 lastMousePos;
			public void RenderAfterWorld(World world)
			{
				var ml = minelayer.Trait<Minelayer>();
				var movement = minelayer.Trait<IMove>();
				var minefield = GetMinefieldCells(ml.minefieldStart, lastMousePos, minelayer.Info.Traits.Get<MinelayerInfo>().MinefieldDepth)
					.Where(p => movement.CanEnterCell(p)).ToArray();

				world.WorldRenderer.DrawLocus(Color.Cyan, minefield);
			}

			public void RenderBeforeWorld(World world) { }

			public string GetCursor(World world, int2 xy, MouseInput mi) { lastMousePos = xy; return "ability"; }	/* todo */
		}

		public void RenderAfterWorld(Actor self)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;
			
			if (minefield != null)
				self.World.WorldRenderer.DrawLocus(Color.Cyan, minefield);
		}
	}
}
