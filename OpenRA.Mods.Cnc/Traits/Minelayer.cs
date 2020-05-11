#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class MinelayerInfo : TraitInfo, Requires<RearmableInfo>
	{
		[ActorReference]
		public readonly string Mine = "minv";

		public readonly string AmmoPoolName = "primary";

		public readonly WDist MinefieldDepth = new WDist(1536);

		[VoiceReference]
		[Desc("Voice to use when ordered to lay a minefield.")]
		public readonly string Voice = "Action";

		[Desc("Sprite overlay to use for valid minefield cells.")]
		public readonly string TileValidName = "build-valid";

		[Desc("Sprite overlay to use for invalid minefield cells.")]
		public readonly string TileInvalidName = "build-invalid";

		[Desc("Sprite overlay to use for minefield cells hidden behind fog or shroud.")]
		public readonly string TileUnknownName = "build-unknown";

		public override object Create(ActorInitializer init) { return new Minelayer(init.Self, this); }
	}

	public class Minelayer : IIssueOrder, IResolveOrder, ISync, IIssueDeployOrder, IOrderVoice, ITick
	{
		public readonly MinelayerInfo Info;

		public readonly Sprite Tile;

		[Sync]
		CPos minefieldStart;

		public Minelayer(Actor self, MinelayerInfo info)
		{
			Info = info;
			var tileset = self.World.Map.Tileset.ToLowerInvariant();
			if (self.World.Map.Rules.Sequences.HasSequence("overlay", "{0}-{1}".F(Info.TileValidName, tileset)))
				Tile = self.World.Map.Rules.Sequences.GetSequence("overlay", "{0}-{1}".F(Info.TileValidName, tileset)).GetSprite(0);
			else
				Tile = self.World.Map.Rules.Sequences.GetSequence("overlay", Info.TileValidName).GetSprite(0);
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
						self.World.OrderGenerator = new MinefieldOrderGenerator(self, start, queued);

					return new Order("BeginMinefield", self, Target.FromCell(self.World, start), queued);
				case "PlaceMine":
					return new Order("PlaceMine", self, Target.FromCell(self.World, self.Location), queued);
				default:
					return null;
			}
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("PlaceMine", self, Target.FromCell(self.World, self.Location), queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return true; }

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "BeginMinefield" && order.OrderString != "PlaceMinefield" && order.OrderString != "PlaceMine")
				return;

			var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
			if (order.OrderString == "BeginMinefield")
				minefieldStart = cell;
			else if (order.OrderString == "PlaceMine")
				self.QueueActivity(order.Queued, new LayMines(self));
			else if (order.OrderString == "PlaceMinefield")
			{
				var movement = self.Trait<IPositionable>();

				var minefield = GetMinefieldCells(minefieldStart, cell, Info.MinefieldDepth)
					.Where(c => movement.CanEnterCell(c, null, BlockedByActor.Immovable) || !self.Owner.Shroud.IsVisible(c))
					.OrderBy(c => (c - minefieldStart).LengthSquared).ToList();

				self.QueueActivity(order.Queued, new LayMines(self, minefield));
				self.ShowTargetLines();
			}
		}

		void ITick.Tick(Actor self)
		{
			if (self.CurrentActivity != null)
				foreach (var field in self.CurrentActivity.ActivitiesImplementing<LayMines>())
					field.CleanMineField(self);
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "PlaceMine" || order.OrderString == "PlaceMinefield")
				return Info.Voice;

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

		class MinefieldOrderGenerator : OrderGenerator
		{
			readonly List<Actor> minelayers;
			readonly Sprite tileOk;
			readonly Sprite tileUnknown;
			readonly Sprite tileBlocked;
			readonly CPos minefieldStart;
			readonly bool queued;

			public MinefieldOrderGenerator(Actor a, CPos xy, bool queued)
			{
				minelayers = new List<Actor>() { a };
				minefieldStart = xy;
				this.queued = queued;

				var minelayer = a.Trait<Minelayer>();
				var tileset = a.World.Map.Tileset.ToLowerInvariant();
				if (a.World.Map.Rules.Sequences.HasSequence("overlay", "{0}-{1}".F(minelayer.Info.TileValidName, tileset)))
					tileOk = a.World.Map.Rules.Sequences.GetSequence("overlay", "{0}-{1}".F(minelayer.Info.TileValidName, tileset)).GetSprite(0);
				else
					tileOk = a.World.Map.Rules.Sequences.GetSequence("overlay", minelayer.Info.TileValidName).GetSprite(0);

				if (a.World.Map.Rules.Sequences.HasSequence("overlay", "{0}-{1}".F(minelayer.Info.TileUnknownName, tileset)))
					tileUnknown = a.World.Map.Rules.Sequences.GetSequence("overlay", "{0}-{1}".F(minelayer.Info.TileUnknownName, tileset)).GetSprite(0);
				else
					tileUnknown = a.World.Map.Rules.Sequences.GetSequence("overlay", minelayer.Info.TileUnknownName).GetSprite(0);

				if (a.World.Map.Rules.Sequences.HasSequence("overlay", "{0}-{1}".F(minelayer.Info.TileInvalidName, tileset)))
					tileBlocked = a.World.Map.Rules.Sequences.GetSequence("overlay", "{0}-{1}".F(minelayer.Info.TileInvalidName, tileset)).GetSprite(0);
				else
					tileBlocked = a.World.Map.Rules.Sequences.GetSequence("overlay", minelayer.Info.TileInvalidName).GetSprite(0);
			}

			public void AddMinelayer(Actor a, CPos xy)
			{
				minelayers.Add(a);
			}

			protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				if (mi.Button == Game.Settings.Game.MouseButtonPreference.Cancel)
				{
					world.CancelInputMode();
					yield break;
				}

				if (mi.Button == Game.Settings.Game.MouseButtonPreference.Action)
				{
					minelayers.First().World.CancelInputMode();
					foreach (var minelayer in minelayers)
						yield return new Order("PlaceMinefield", minelayer, Target.FromCell(world, cell), queued);
				}
			}

			protected override void Tick(World world)
			{
				minelayers.RemoveAll(minelayer => !minelayer.IsInWorld || minelayer.IsDead);
				if (!minelayers.Any())
					world.CancelInputMode();
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
			protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
			{
				var minelayer = minelayers.FirstOrDefault(m => m.IsInWorld && !m.IsDead);
				if (minelayer == null)
					yield break;

				// We get the biggest depth so we cover all cells that mines could be placed on.
				var lastMousePos = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var minefield = GetMinefieldCells(minefieldStart, lastMousePos,
					minelayers.Max(m => m.Info.TraitInfo<MinelayerInfo>().MinefieldDepth));

				var movement = minelayer.Trait<IPositionable>();
				var mobile = movement as Mobile;
				var pal = wr.Palette(TileSet.TerrainPaletteInternalName);
				foreach (var c in minefield)
				{
					var tile = tileOk;
					if (world.FogObscures(c))
						tile = tileUnknown;
					else if (!movement.CanEnterCell(c, null, BlockedByActor.Immovable) || (mobile != null && !mobile.CanStayInCell(c)))
						tile = tileBlocked;

					yield return new SpriteRenderable(tile, world.Map.CenterOfCell(c),
						WVec.Zero, -511, pal, 1f, true);
				}
			}

			protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world) { yield break; }

			protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				return "ability";
			}
		}

		class BeginMinefieldOrderTargeter : IOrderTargeter
		{
			public string OrderID { get { return "BeginMinefield"; } }
			public int OrderPriority { get { return 5; } }
			public bool TargetOverridesSelection(Actor self, Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers) { return true; }

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				if (target.Type != TargetType.Terrain)
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);
				if (!self.World.Map.Contains(location))
					return false;

				cursor = "ability";
				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				return modifiers.HasModifier(TargetModifiers.ForceAttack);
			}

			public bool IsQueued { get; protected set; }
		}
	}
}
