#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	public class MinelayerInfo : ITraitInfo, Requires<RearmableInfo>
	{
		[ActorReference]
		public readonly string Mine = "minv";

		public readonly string AmmoPoolName = "primary";

		public readonly WDist MinefieldDepth = new WDist(1536);

		[VoiceReference]
		[Desc("Voice to use when ordered to lay a minefield.")]
		public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new Minelayer(init.Self, this); }
	}

	public class Minelayer : IIssueOrder, IResolveOrder, ISync, IIssueDeployOrder, IOrderVoice, ITick
	{
		public readonly MinelayerInfo Info;

		public readonly Sprite TileOk;
		public readonly Sprite TileBlocked;

		public Minelayer(Actor self, MinelayerInfo info)
		{
			Info = info;

			var tileset = self.World.Map.Tileset.ToLowerInvariant();
			if (self.World.Map.Rules.Sequences.HasSequence("overlay", "build-valid-{0}".F(tileset)))
				TileOk = self.World.Map.Rules.Sequences.GetSequence("overlay", "build-valid-{0}".F(tileset)).GetSprite(0);
			else
				TileOk = self.World.Map.Rules.Sequences.GetSequence("overlay", "build-valid").GetSprite(0);

			TileBlocked = self.World.Map.Rules.Sequences.GetSequence("overlay", "build-invalid").GetSprite(0);
		}

		IEnumerable<IOrderTargeter> IIssueOrder.OrderTargeters
		{
			get
			{
				yield return new MinefieldOrderTargeter(this);
				yield return new DeployOrderTargeter("PlaceMine", 5);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued, CPos extraLoc)
		{
			switch (order.OrderID)
			{
				case "PlaceMinefield":
					return new Order("PlaceMinefield", self, target, queued)
					{
						ExtraLocation = extraLoc
					};
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

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self) { return true; }

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "PlaceMinefield" && order.OrderString != "PlaceMine")
				return;

			var minefieldStart = self.World.Map.CellContaining(order.Target.CenterPosition);

			if (order.OrderString == "PlaceMine")
				self.QueueActivity(order.Queued, new LayMines(self));
			else if (order.OrderString == "PlaceMinefield")
			{
				var movement = self.Trait<IPositionable>();
				var minefield = GetMinefieldCells(minefieldStart, order.ExtraLocation, Info.MinefieldDepth)
					.Where(c => movement.CanEnterCell(c, null, BlockedByActor.None))
					.OrderBy(c => (c - minefieldStart).LengthSquared).ToList();

				self.QueueActivity(order.Queued, new LayMines(self, minefield));
				self.ShowTargetLines();
			}
		}

		void ITick.Tick(Actor self)
		{
			if (self.CurrentActivity != null)
				foreach (var field in self.CurrentActivity.ActivitiesImplementing<LayMines>())
					field.CleanPlacedMines(self);
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

		class MinefieldOrderTargeter : IOrderTargeter
		{
			readonly Sprite tileOk;
			readonly Sprite tileBlocked;

			public string OrderID { get { return "PlaceMinefield"; } }
			public int OrderPriority { get { return 5; } }
			public bool TargetOverridesSelection(Actor self, Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers) { return true; }
			public bool CanDrag { get { return true; } }

			public MinefieldOrderTargeter(Minelayer minelayer)
			{
				tileOk = minelayer.TileOk;
				tileBlocked = minelayer.TileBlocked;
			}

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

			public IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world, Actor self, Target target)
			{
				var minelayers = world.Selection.Actors.Where(a => a.IsInWorld && !a.IsDead && a.TraitOrDefault<Minelayer>() != null);
				if (!minelayers.Any() || self != minelayers.First())
					yield break;

				// We get the biggest depth so we cover all cells that mines could be placed on.
				var lastMousePos = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var startPos = world.Map.CellContaining(target.CenterPosition);
				var minefield = GetMinefieldCells(startPos, lastMousePos,
					minelayers.Max(m => m.Info.TraitInfo<MinelayerInfo>().MinefieldDepth));

				var movement = self.Trait<IPositionable>();
				var pal = wr.Palette(TileSet.TerrainPaletteInternalName);
				foreach (var c in minefield)
				{
					var tile = movement.CanEnterCell(c, null, BlockedByActor.None) && !world.ShroudObscures(c) ? tileOk : tileBlocked;
					yield return new SpriteRenderable(tile, world.Map.CenterOfCell(c),
						WVec.Zero, -511, pal, 1f, true);
				}
			}

			public bool IsQueued { get; protected set; }
		}
	}
}
