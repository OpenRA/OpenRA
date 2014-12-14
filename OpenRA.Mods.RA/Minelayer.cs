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
using System.Linq;
using System.Collections.Generic;
using OpenRA.Traits;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.Common.Orders;

namespace OpenRA.Mods.RA
{
	class MinelayerInfo : ITraitInfo
	{
		[ActorReference] public readonly string Mine = "minv";
		[ActorReference] public readonly string[] RearmBuildings = { "fix" };

		public readonly string RearmSound = "minelay1.aud";
		public readonly int Payload = 3;
		public readonly int ReloadTicks = 25 * 2;
		[Desc("Defaults to value in Payload.")]
		public readonly int PipCount = 3;
		public readonly PipType PipType = PipType.Green;
		public readonly PipType PipTypeEmpty = PipType.Transparent;

		public readonly float MinefieldDepth = 1.5f;

		public object Create(ActorInitializer init) { return new Minelayer(init.self, this); }
	}

	class Minelayer : IIssueOrder, IResolveOrder, IPostRenderSelection, IPips, ISync
	{
		/* TODO: [Sync] when sync can cope with arrays! */
		public CPos[] Minefield = null;
		[Sync] int payload;
		[Sync] CPos minefieldStart;
		Actor self;
		public MinelayerInfo Info;
		Sprite tile;

		public Minelayer(Actor self, MinelayerInfo info)
		{
			this.self = self;
			this.Info = info;

			var tileset = self.World.TileSet.Id.ToLowerInvariant();
			tile = self.World.Map.SequenceProvider.GetSequence("overlay", "build-valid-{0}".F(tileset)).GetSprite(0);
			payload = info.Payload;
		}

		public bool FullPayload() { return payload == Info.Payload; }
		public bool HasPayload() { return payload > 0; }
		public bool GivePayload()
		{
			if (payload >= Info.Payload) return false;
			++payload;
			return true;
		}

		public bool TakePayload()
		{
			if (payload <= 0) return false;
			--payload;
			return true;
		}

		public int ReloadTimePerPayload() { return Info.ReloadTicks; }

//		public void Attacking(Actor self, Target target, Armament a, Barrel barrel) { TakePayload(); }

		public int GetPayloadCount() { return payload; }

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var pips = Info.PipCount != 0 ? Info.PipCount : Info.Payload;
			return Exts.MakeArray(pips,
				i => (payload * pips) / Info.Payload > i ? Info.PipType : Info.PipTypeEmpty);
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new BeginMinefieldOrderTargeter();
				yield return new DeployOrderTargeter("PlaceMine", 5);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			switch (order.OrderID)
			{
				case "BeginMinefield":
					var start = self.World.Map.CellContaining(target.CenterPosition);
					self.World.OrderGenerator = new MinefieldOrderGenerator(self, start);
					return new Order("BeginMinefield", self, false) { TargetLocation = start };
				case "PlaceMine":
					return new Order("PlaceMine", self, false) { TargetLocation = self.Location };
				default:
					return null;
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "BeginMinefield")
				minefieldStart = order.TargetLocation;

			if (order.OrderString == "PlaceMine")
			{
				minefieldStart = order.TargetLocation;
				Minefield = new CPos[] { order.TargetLocation };
				self.CancelActivity();
				self.QueueActivity(new LayMines());
			}

			if (order.OrderString == "PlaceMinefield")
			{
				var movement = self.Trait<IPositionable>();

				Minefield = GetMinefieldCells(minefieldStart, order.TargetLocation,
					self.Info.Traits.Get<MinelayerInfo>().MinefieldDepth)
					.Where(p => movement.CanEnterCell(p, null, false)).ToArray();

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
			var c = -float2.Dot(q, new float2(start.X, start.Y));

			/* return all points such that |ax + by + c| < depth */

			for (var i = mins.X; i <= maxs.X; i++)
				for (var j = mins.Y; j <= maxs.Y; j++)
					if (Math.Abs(q.X * i + q.Y * j + c) < depth)
						yield return new CPos(i, j);
		}

		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr)
		{
			if (self.Owner != self.World.LocalPlayer || Minefield == null)
				yield break;

			var pal = wr.Palette("terrain");
			foreach (var c in Minefield)
				yield return new SpriteRenderable(tile, self.World.Map.CenterOfCell(c),
					WVec.Zero, -511, pal, 1f, true);
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
				if (mi.Button == MouseButton.Left)
				{
					world.CancelInputMode();
					yield break;
				}

				var underCursor = world.ScreenMap.ActorsAt(mi)
					.Where(a => !world.FogObscures(a))
					.MaxByOrDefault(a => a.Info.Traits.Contains<SelectableInfo>()
						? a.Info.Traits.Get<SelectableInfo>().Priority : int.MinValue);

				if (mi.Button == MouseButton.Right && underCursor == null)
				{
					minelayer.World.CancelInputMode();
					yield return new Order("PlaceMinefield", minelayer, false) { TargetLocation = xy };
				}
			}

			public void Tick(World world)
			{
				if (!minelayer.IsInWorld || minelayer.IsDead)
					world.CancelInputMode();
			}

			CPos lastMousePos;
			public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
			public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr, World world)
			{
				if (!minelayer.IsInWorld)
					yield break;

				var movement = minelayer.Trait<IPositionable>();
				var minefield = GetMinefieldCells(minefieldStart, lastMousePos,
					minelayer.Info.Traits.Get<MinelayerInfo>().MinefieldDepth);

				var pal = wr.Palette("terrain");
				foreach (var c in minefield)
				{
					var tile = movement.CanEnterCell(c, null, false) ? tileOk : tileBlocked;
					yield return new SpriteRenderable(tile, world.Map.CenterOfCell(c),
						WVec.Zero, -511, pal, 1f, true);
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

				var location = self.World.Map.CellContaining(target.CenterPosition);
				if (!self.World.Map.Contains(location))
					return false;

				cursor = "ability";
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				return !othersAtTarget.Any() && modifiers.HasModifier(TargetModifiers.ForceAttack);
			}

			public bool IsQueued { get; protected set; }
		}
	}
}
