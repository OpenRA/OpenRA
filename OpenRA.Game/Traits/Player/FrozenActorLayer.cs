#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Traits
{
	public class FrozenActorLayerInfo : ITraitInfo
	{
		[Desc("Size of partition bins (screen pixels)")]
		public readonly int BinSize = 250;

		public object Create(ActorInitializer init) { return new FrozenActorLayer(init.world, this); }
	}

	public class FrozenActor
	{
		public readonly IEnumerable<CPos> Footprint;
		public readonly WPos CenterPosition;
		public readonly Rectangle Bounds;
		readonly Actor actor;

		public IRenderable[] Renderables { set; private get; }
		public Player Owner;

		public string TooltipName;
		public Player TooltipOwner;

		public int HP;
		public DamageState DamageState;

		public bool Visible;

		public FrozenActor(Actor self, IEnumerable<CPos> footprint)
		{
			actor = self;
			Footprint = footprint;
			CenterPosition = self.CenterPosition;
			Bounds = self.Bounds.Value;
		}

		public uint ID { get { return actor.ActorID; } }
		public bool IsValid { get { return Owner != null; } }
		public ActorInfo Info { get { return actor.Info; } }
		public Actor Actor { get { return !actor.IsDead() ? actor : null; } }

		int flashTicks;
		public void Tick(World world, Shroud shroud)
		{
			Visible = !Footprint.Any(c => shroud.IsVisible(c));

			if (flashTicks > 0)
				flashTicks--;
		}

		public void Flash()
		{
			flashTicks = 5;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (Renderables == null)
				return SpriteRenderable.None;

			if (flashTicks > 0 && flashTicks % 2 == 0)
			{
				var highlight = wr.Palette("highlight");
				return Renderables.Concat(Renderables.Where(r => !r.IsDecoration)
					.Select(r => r.WithPalette(highlight)));
			}
			return Renderables;
		}

		public bool HasRenderables { get { return Renderables != null; } }
	}

	public class FrozenActorLayer : IRender, ITick, ISync
	{
		[Sync] public int VisibilityHash;
		[Sync] public int FrozenHash;

		readonly FrozenActorLayerInfo info;
		Dictionary<uint, FrozenActor> frozen;
		List<FrozenActor>[,] bins;

		public FrozenActorLayer(World world, FrozenActorLayerInfo info)
		{
			this.info = info;
			frozen = new Dictionary<uint, FrozenActor>();
			bins = new List<FrozenActor>[
				world.Map.MapSize.X * Game.CellSize / info.BinSize,
				world.Map.MapSize.Y * Game.CellSize / info.BinSize];

			for (var j = 0; j <= bins.GetUpperBound(1); j++)
				for (var i = 0; i <= bins.GetUpperBound(0); i++)
					bins[i, j] = new List<FrozenActor>();
		}

		public void Add(FrozenActor fa)
		{
			frozen.Add(fa.ID, fa);

			var top = (int)Math.Max(0, fa.Bounds.Top / info.BinSize);
			var left = (int)Math.Max(0, fa.Bounds.Left / info.BinSize);
			var bottom = (int)Math.Min(bins.GetUpperBound(1), fa.Bounds.Bottom / info.BinSize);
			var right = (int)Math.Min(bins.GetUpperBound(0), fa.Bounds.Right / info.BinSize);
			for (var j = top; j <= bottom; j++)
				for (var i = left; i <= right; i++)
					bins[i, j].Add(fa);
		}

		public void Tick(Actor self)
		{
			var remove = new List<uint>();
			VisibilityHash = 0;
			FrozenHash = 0;

			foreach (var kv in frozen)
			{
				FrozenHash += (int)kv.Key;

				kv.Value.Tick(self.World, self.Owner.Shroud);
				if (kv.Value.Visible)
					VisibilityHash += (int)kv.Key;

				if (!kv.Value.Visible && kv.Value.Actor == null)
					remove.Add(kv.Key);
			}

			foreach (var r in remove)
			{
				foreach (var bin in bins)
					bin.Remove(frozen[r]);
				frozen.Remove(r);
			}
		}

		public virtual IEnumerable<IRenderable> Render(Actor self, WorldRenderer wr)
		{
			return frozen.Values
				.Where(f => f.Visible)
				.SelectMany(ff => ff.Render(wr));
		}

		public IEnumerable<FrozenActor> FrozenActorsAt(int2 pxPos)
		{
			var x = (pxPos.X / info.BinSize).Clamp(0, bins.GetUpperBound(0));
			var y = (pxPos.Y / info.BinSize).Clamp(0, bins.GetUpperBound(1));
			return bins[x, y].Where(p => p.Bounds.Contains(pxPos) && p.IsValid);
		}

		public FrozenActor FromID(uint id)
		{
			FrozenActor ret;
			if (!frozen.TryGetValue(id, out ret))
				return null;

			return ret;
		}
	}
}
