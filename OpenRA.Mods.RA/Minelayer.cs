#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class MinelayerInfo : ITraitInfo
	{
		[ActorReference] public readonly string Mine = "minv";
		[ActorReference] public readonly string[] RearmBuildings = { "fix" };

		public readonly float MinefieldDepth = 1.5f;

		public object Create(ActorInitializer init) { return new Minelayer(init.self); }
	}

	class Minelayer : IIssueOrder, IResolveOrder, IPostRenderSelection, ISync
	{
		/* [Sync] when sync can cope with arrays! */
		public CPos[] minefield = null;
		[Sync] CPos minefieldStart;
		Actor self;
		Sprite tile;

		public Minelayer(Actor self)
		{
			this.self = self;

			var tileset = self.World.TileSet.Id.ToLowerInvariant();
			tile = self.World.Map.SequenceProvider.GetSequence("overlay", "build-valid-{0}".F(tileset)).GetSprite(0);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new BeginMinefieldOrderTargeter(); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (!(order is BeginMinefieldOrderTargeter))
				return null;

			var start = target.CenterPosition.ToCPos();
			self.World.OrderGenerator = new MinefieldOrderGenerator(self, start);
			return new Order("BeginMinefield", self, false) { TargetLocation = start };
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "BeginMinefield")
				minefieldStart = order.TargetLocation;

			if (order.OrderString == "PlaceMinefield")
			{
				var movement = self.Trait<IPositionable>();

				minefield = GetMinefieldCells(minefieldStart, order.TargetLocation,
					self.Info.Traits.Get<MinelayerInfo>().MinefieldDepth)
					.Where(p => movement.CanEnterCell(p)).ToArray();

				self.CancelActivity();
				self.QueueActivity(new LayMines());
			}
		}

		static IEnumerable<CPos> GetMinefieldCells(CPos start, CPos end, float depth)
		{
			var mins = CPos.Min(start, end);
			var maxs = CPos.Max(start, end);

			/* TODO: proper endcaps, if anyone cares (which won't happen unless depth is large) */

			var p = end - start;
			var q = new float2(p.Y, -p.X);
			q = (start != end) ? (1 / q.Length) * q : new float2(1, 0);
			var c = -float2.Dot(q, start.ToInt2());

			/* return all points such that |ax + by + c| < depth */

			for (var i = mins.X; i <= maxs.X; i++)
				for (var j = mins.Y; j <= maxs.Y; j++)
					if (Math.Abs(q.X * i + q.Y * j + c) < depth)
						yield return new CPos(i, j);
		}

		public void RenderAfterWorld(WorldRenderer wr)
		{
			if (self.Owner != self.World.LocalPlayer || minefield == null)
				return;

			var pal = wr.Palette("terrain");
			foreach (var c in minefield)
				new SpriteRenderable(tile, c.CenterPosition,
					WVec.Zero, -511, pal, 1f, true).Render(wr);
		}

		class MinefieldOrderGenerator : IOrderGenerator
		{
			readonly Actor minelayer;
			readonly CPos minefieldStart;
			readonly Sprite tileOk;
			readonly Sprite tileBlocked;

			public MinefieldOrderGenerator(Actor self, CPos xy)
			{
				minelayer = self;
				minefieldStart = xy;

				var tileset = self.World.TileSet.Id.ToLowerInvariant();
				tileOk = self.World.Map.SequenceProvider.GetSequence("overlay", "build-valid-{0}".F(tileset)).GetSprite(0);
				tileBlocked = self.World.Map.SequenceProvider.GetSequence("overlay", "build-invalid").GetSprite(0);
			}

			public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
			{
				if (mi.Button == Game.mouseButtonPreference.Cancel)
				{
					world.CancelInputMode();
					yield break;
				}

				var underCursor = world.ScreenMap.ActorsAt(mi)
					.Where(a => !world.FogObscures(a))
					.OrderByDescending(a => a.Info.Traits.Contains<SelectableInfo>()
						? a.Info.Traits.Get<SelectableInfo>().Priority : int.MinValue)
					.FirstOrDefault();

				if (mi.Button == Game.mouseButtonPreference.Action && underCursor == null)
				{
					minelayer.World.CancelInputMode();
					yield return new Order("PlaceMinefield", minelayer, false) { TargetLocation = xy };
				}
			}

			public void Tick(World world)
			{
				if (!minelayer.IsInWorld || minelayer.IsDead())
					world.CancelInputMode();
			}

			CPos lastMousePos;
			public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
			public void RenderAfterWorld(WorldRenderer wr, World world)
			{
				if (!minelayer.IsInWorld)
					return;

				var movement = minelayer.Trait<IPositionable>();
				var minefield = GetMinefieldCells(minefieldStart, lastMousePos,
					minelayer.Info.Traits.Get<MinelayerInfo>().MinefieldDepth);

				var pal = wr.Palette("terrain");
				foreach (var c in minefield)
				{
					var tile = movement.CanEnterCell(c) ? tileOk : tileBlocked;
					new SpriteRenderable(tile, c.CenterPosition,
						WVec.Zero, -511, pal, 1f, true).Render(wr);
				}
			}

			public string GetCursor(World world, CPos xy, MouseInput mi) { lastMousePos = xy; return "ability"; }	/* TODO */
		}

		class BeginMinefieldOrderTargeter : IOrderTargeter
		{
			public string OrderID { get { return "BeginMinefield"; } }
			public int OrderPriority { get { return 5; } }

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, TargetModifiers modifiers, ref string cursor)
			{
				if (target.Type != TargetType.Terrain)
					return false;

				var location = target.CenterPosition.ToCPos();
				if (!self.World.Map.IsInMap(location))
					return false;

				cursor = "ability";
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				return !othersAtTarget.Any() && modifiers.HasModifier(TargetModifiers.ForceAttack);
			}

			public bool IsQueued { get; protected set; }
		}
	}
}
