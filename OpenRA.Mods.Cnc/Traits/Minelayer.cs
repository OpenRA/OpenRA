#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class MinelayerInfo : ITraitInfo
	{
		[ActorReference] public readonly string Mine = "minv";
		[ActorReference] public readonly HashSet<string> RearmBuildings = new HashSet<string> { "fix" };

		public readonly string AmmoPoolName = "primary";

		public readonly WDist MinefieldDepth = new WDist(1536);

		[Desc("Voice to use when ordered to lay a minefield.")]
		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new Minelayer(init.Self, this); }
	}

	public class Minelayer : IIssueOrder, IResolveOrder, IRenderAboveShroudWhenSelected, ISync, IIssueDeployOrder, IOrderVoice
	{
		readonly MinelayerInfo info;

		/* TODO: [Sync] when sync can cope with arrays! */
		public CPos[] Minefield = null;
		readonly Sprite tile;
		[Sync] CPos minefieldStart;

		public Minelayer(Actor self, MinelayerInfo info)
		{
			this.info = info;

			var tileset = self.World.Map.Tileset.ToLowerInvariant();
			tile = self.World.Map.Rules.Sequences.GetSequence("overlay", "build-valid-{0}".F(tileset)).GetSprite(0);
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new BeginMinefieldOrderTargeter();
				yield return new DeployOrderTargeter("PlaceMine", 5);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			switch (order.OrderID)
			{
				case "BeginMinefield":
					var start = self.World.Map.CellContaining(target.CenterPosition);
					if (self.World.OrderGenerator is MinefieldOrderGenerator)
						((MinefieldOrderGenerator)self.World.OrderGenerator).AddMinelayer(self, start);
					else
						self.World.OrderGenerator = new MinefieldOrderGenerator(self, start);

					return new Order("BeginMinefield", self, Target.FromCell(self.World, start), false);
				case "PlaceMine":
					return new Order("PlaceMine", self, Target.FromCell(self.World, self.Location), false);
				default:
					return null;
			}
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self)
		{
			return new Order("PlaceMine", self, Target.FromCell(self.World, self.Location), false);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self) { return true; }

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "BeginMinefield")
				minefieldStart = order.TargetLocation;

			if (order.OrderString == "PlaceMine")
			{
				minefieldStart = order.TargetLocation;
				Minefield = new CPos[] { order.TargetLocation };
				self.CancelActivity();
				self.QueueActivity(new LayMines(self));
			}

			if (order.OrderString == "PlaceMinefield")
			{
				var movement = self.Trait<IPositionable>();

				Minefield = GetMinefieldCells(minefieldStart, order.TargetLocation, info.MinefieldDepth)
					.Where(p => movement.CanEnterCell(p, null, false)).ToArray();

				if (Minefield.Length == 1 && Minefield[0] != self.Location)
					self.SetTargetLine(Target.FromCell(self.World, Minefield[0]), Color.Red);

				self.CancelActivity();
				self.QueueActivity(new LayMines(self));
			}
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "PlaceMine" || order.OrderString == "PlaceMinefield")
				return info.Voice;

			return null;
		}

		static IEnumerable<CPos> GetMinefieldCells(CPos start, CPos end, WDist depth)
		{
			var mins = new CPos(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y));
			var maxs = new CPos(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));

			// TODO: proper endcaps, if anyone cares (which won't happen unless depth is large)
			var p = end - start;
			var q = new float2(p.Y, -p.X);
			q = (start != end) ? (1 / q.Length) * q : new float2(1, 0);
			var c = -float2.Dot(q, new float2(start.X, start.Y));

			// return all points such that |ax + by + c| < depth
			// HACK: This will return the wrong results for isometric cells
			for (var i = mins.X; i <= maxs.X; i++)
				for (var j = mins.Y; j <= maxs.Y; j++)
					if (Math.Abs(q.X * i + q.Y * j + c) * 1024 < depth.Length)
						yield return new CPos(i, j);
		}

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (self.Owner != self.World.LocalPlayer || Minefield == null)
				yield break;

			// Single-cell mine fields use a target line instead
			if (Minefield.Length == 1)
				yield break;

			var pal = wr.Palette(TileSet.TerrainPaletteInternalName);
			foreach (var c in Minefield)
				yield return new SpriteRenderable(tile, self.World.Map.CenterOfCell(c),
					WVec.Zero, -511, pal, 1f, true);
		}

		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable { get { return false; } }

		class MinefieldOrderGenerator : IOrderGenerator
		{
			readonly List<Actor> minelayers;
			readonly Sprite tileOk;
			readonly Sprite tileBlocked;
			readonly CPos minefieldStart;

			public MinefieldOrderGenerator(Actor a, CPos xy)
			{
				minelayers = new List<Actor>() { a };
				minefieldStart = xy;

				var tileset = a.World.Map.Tileset.ToLowerInvariant();
				tileOk = a.World.Map.Rules.Sequences.GetSequence("overlay", "build-valid-{0}".F(tileset)).GetSprite(0);
				tileBlocked = a.World.Map.Rules.Sequences.GetSequence("overlay", "build-invalid").GetSprite(0);
			}

			public void AddMinelayer(Actor a, CPos xy)
			{
				minelayers.Add(a);
			}

			public IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				if (mi.Button == Game.Settings.Game.MouseButtonPreference.Cancel)
				{
					world.CancelInputMode();
					yield break;
				}

				var underCursor = world.ScreenMap.ActorsAtMouse(mi)
					.Select(a => a.Actor)
					.Where(a => !world.FogObscures(a))
					.MaxByOrDefault(a => a.Info.HasTraitInfo<SelectableInfo>()
						? a.Info.TraitInfo<SelectableInfo>().Priority : int.MinValue);

				if (mi.Button == Game.Settings.Game.MouseButtonPreference.Action && underCursor == null)
				{
					minelayers.First().World.CancelInputMode();
					foreach (var minelayer in minelayers)
						yield return new Order("PlaceMinefield", minelayer, Target.FromCell(world, cell), false);
				}
			}

			public void Tick(World world)
			{
				minelayers.RemoveAll(minelayer => !minelayer.IsInWorld || minelayer.IsDead);
				if (!minelayers.Any())
					world.CancelInputMode();
			}

			public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
			public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
			{
				var minelayer = minelayers.FirstOrDefault(m => m.IsInWorld && !m.IsDead);
				if (minelayer == null)
					yield break;

				// We get the biggest depth so we cover all cells that mines could be placed on.
				var lastMousePos = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var minefield = GetMinefieldCells(minefieldStart, lastMousePos,
					minelayers.Max(m => m.Info.TraitInfo<MinelayerInfo>().MinefieldDepth));

				var movement = minelayer.Trait<IPositionable>();
				var pal = wr.Palette(TileSet.TerrainPaletteInternalName);
				foreach (var c in minefield)
				{
					var tile = movement.CanEnterCell(c, null, false) && !world.ShroudObscures(c) ? tileOk : tileBlocked;
					yield return new SpriteRenderable(tile, world.Map.CenterOfCell(c),
						WVec.Zero, -511, pal, 1f, true);
				}
			}

			public string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				return "ability";
			}
		}

		class BeginMinefieldOrderTargeter : IOrderTargeter
		{
			public string OrderID { get { return "BeginMinefield"; } }
			public int OrderPriority { get { return 5; } }
			public bool TargetOverridesSelection(TargetModifiers modifiers) { return true; }

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
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
